using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Infra.EventBus.Logging;
using NetCoreEventBus.Infra.EventBus.RabbitMQ.Connection;
using NetCoreEventBus.Infra.EventBus.Subscriptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NetCoreEventBus.Infra.EventBus.RabbitMQ.Bus;

public sealed class RabbitMqConsumer : IDisposable
{
    private const int MaxWaitDisposeTime = 30000;
    private readonly TimeSpan _subscribeRetryTime = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly IEventBusLogger _logger;

    private static readonly object ChannelAckResourceLock = new();
    private readonly SemaphoreSlim consumerPrefetchSemaphore;
    public readonly IModel ConsumerChannel;
    private bool _disposed;

    public RabbitMqConsumer(IServiceScopeFactory serviceScopeFactory,
        IRabbitMqPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        RabbitMqEventBusConfig rabbitMqEventBusConfig,
        IEventBusLogger logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory),"MessageBroker ServiceScopeFactory is null");
        _persistentConnection = persistentConnection;
        _subscriptionsManager = subscriptionsManager;
        _rabbitMqEventBusConfig = rabbitMqEventBusConfig;
        _logger = logger;

        ConsumerChannel = CreateConsumerChannel();
        consumerPrefetchSemaphore = new SemaphoreSlim(_rabbitMqEventBusConfig.ConsumerMaxFetchCount);
    }

    public void StartBasicConsume(string eventName)
    {
        _logger.LogInformation("Creating RabbitMQ consumer channel...");

        if (ConsumerChannel == null)
        {
            _logger.LogError("Could not start basic consume because consumer channel is null.");
            return;
        }

        _logger.LogInformation("Starting RabbitMQ basic consume...");
        ConsumerChannel.BasicQos(0, _rabbitMqEventBusConfig.ConsumerMaxFetchCount, false);
        var consumer = new AsyncEventingBasicConsumer(ConsumerChannel);
        consumer.Received += ConsumerReceived;
        ConsumerChannel.BasicConsume(queue: GetConsumerQueueName(eventName), autoAck: false, consumer: consumer);
    }

    private async Task ConsumerReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_disposed)
        {
            // don't use semaphore count until disposed function semaphore count check 
            while (true) { Thread.Sleep(1000); }
        }

        await consumerPrefetchSemaphore.WaitAsync();

        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        Task.Run(() =>
        {
            bool isAcknowledged = false;

            _logger.LogInformation($"[ConsumerTag: {(sender as AsyncEventingBasicConsumer).ConsumerTags.FirstOrDefault() ?? string.Empty}]");
            try
            {
                //Console.WriteLine($"[ConsumerTag: {(ch as EventingBasicConsumer).ConsumerTag}]  [{DateTime.Now}]  [Message: {message}]  [Thread Name: {Thread.CurrentThread.Name}]  [Thread Number: {Thread.CurrentThread.ManagedThreadId}]");

                ProcessEvent(eventName, message);
                lock (ChannelAckResourceLock)
                {
                    ConsumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }

                isAcknowledged = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning( "Error processing the following message: {Message}.", message);
            }
            finally
            {
                if (!isAcknowledged)
                {
                    TryEnqueueMessageAgainAsync(eventArgs).GetAwaiter().GetResult();
                }

                consumerPrefetchSemaphore.Release();
            }
        });
    }

    private async Task TryEnqueueMessageAgainAsync(BasicDeliverEventArgs eventArgs)
    {
        try
        {
            _logger.LogWarning("Adding message to queue again with {Time} seconds delay...", $"{_subscribeRetryTime.TotalSeconds:n1}");

            if (!_disposed)
            {
                await Task.Delay(_subscribeRetryTime);
            }
            lock (ChannelAckResourceLock)
            {
                ConsumerChannel.BasicNack(eventArgs.DeliveryTag, false, true);
            }

            _logger.LogInformation("Message added to queue again.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Could not enqueue message again: {Error}.", ex.Message);
        }
    }

    private void ProcessEvent(string eventName, string message)
    {
        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            throw new Exception("There are no subscriptions for this event.");
        }

        var eventType = _subscriptionsManager.GetEventTypeByName(eventName);
            
        var genericClass = typeof(MessageEnvelope<>);
        var constructedClass = genericClass.MakeGenericType(eventType!);
        var @event = JsonSerializer.Deserialize(message, constructedClass);
        
        var eventHandlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        
        var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);
        // AbcEvent => AbcEventLogHandler, AbcEventMailHandler etc. Multiple subscription can be for one Event
        foreach (var subscription in subscriptions)
        {
            using var scope = _serviceScopeFactory.CreateScope(); // because handler type scoped service
            var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
            if (handler == null)
            {
                _logger.LogWarning("There are no handlers for the following event: {EventName}", eventName);
                continue;
            }

            ((Task)eventHandlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEventMessage>.HandleAsync))?.Invoke(handler, new object[] { @event }))!.GetAwaiter().GetResult();
        }
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

        return channel;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var consumerChannelNumber = ConsumerChannel.ChannelNumber.ToString();
        _logger.LogInformation("Consumer channel [{ChannelNo}] shutting down...", consumerChannelNumber);

        var waitCounter = 0;
        while (waitCounter * 1000 < MaxWaitDisposeTime && consumerPrefetchSemaphore.CurrentCount < _rabbitMqEventBusConfig.ConsumerMaxFetchCount)
        {
            _logger.LogInformation("Consumers channel [{ChannelNo}] Fetch Count [ {Done}/{All} ] => waiting...",
                consumerChannelNumber, consumerPrefetchSemaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerMaxFetchCount);
            Thread.Sleep(1000);
            waitCounter++;
        }

        _logger.LogInformation("Consumers channel [{ChannelNo}] Fetch Count [ {Done}/{All} ] => all fetching done",
            consumerChannelNumber, consumerPrefetchSemaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerMaxFetchCount);

        consumerPrefetchSemaphore?.Dispose();
        ConsumerChannel?.Dispose();

        _logger.LogInformation("Consumer channel [{ChannelNo}] terminated", consumerChannelNumber);
    }

    private string GetConsumerQueueName(string eventName)
    {
        return $"{_rabbitMqEventBusConfig.ClientInfo}_{TrimEventName(eventName)}";
    }

    private string TrimEventName(string eventName)
    {
        if (_rabbitMqEventBusConfig.DeleteEventPrefix && eventName.StartsWith(_rabbitMqEventBusConfig.EventNamePrefix))
        {
            eventName = eventName.Substring(_rabbitMqEventBusConfig.EventNamePrefix.Length);
        }

        if (_rabbitMqEventBusConfig.DeleteEventSuffix && eventName.EndsWith(_rabbitMqEventBusConfig.EventNameSuffix))
        {
            eventName = eventName.Substring(0, eventName.Length - _rabbitMqEventBusConfig.EventNameSuffix.Length);
        }

        return eventName;
    }
}