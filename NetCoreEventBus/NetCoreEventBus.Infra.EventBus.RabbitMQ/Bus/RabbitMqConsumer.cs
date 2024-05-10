using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Infra.EventBus.RabbitMQ.Connection;
using NetCoreEventBus.Infra.EventBus.Subscriptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NetCoreEventBus.Infra.EventBus.RabbitMQ.Bus;

public sealed class RabbitMqConsumer : IDisposable
{
    private const int MaxWaitDisposeTime = 30000;
    private const int prefetchCount = 1;
    private readonly TimeSpan _subscribeRetryTime = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly IPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly ILogger _logger;
    private readonly string _queueName;
    private readonly string _exchangeName;

    private readonly SemaphoreSlim consumerPrefetchSemaphore;
    private readonly IModel _consumerChannel;
    private bool _disposed;

    public RabbitMqConsumer(IServiceProvider serviceProvider, IPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        ILogger logger,
        string queueName, string exchangeName)
    {
        _serviceProvider = serviceProvider;
        _persistentConnection = persistentConnection;
        _subscriptionsManager = subscriptionsManager;
        _logger = logger;
        _queueName = queueName;
        _exchangeName = exchangeName;

        _consumerChannel = CreateConsumerChannel();
        consumerPrefetchSemaphore = new SemaphoreSlim(prefetchCount);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var consumerChannelNumber = _consumerChannel.ChannelNumber.ToString();
        _logger.LogInformation("Consumer channel [{ChannelNo}] queue [{QueueName}] shutting down...", consumerChannelNumber, _queueName);

        var waitCounter = 0;
        while (waitCounter * 1000 < MaxWaitDisposeTime && consumerPrefetchSemaphore.CurrentCount < prefetchCount)
        {
            _logger.LogInformation("Consumers channel [{ChannelNo}] queue [{QueueName}] Fetch Count [ {Done}/{All} ] => waiting...",
                consumerChannelNumber, _queueName, consumerPrefetchSemaphore.CurrentCount, prefetchCount);
            Thread.Sleep(1000);
            waitCounter++;
        }

        _logger.LogInformation("Consumers channel [{ChannelNo}] queue [{QueueName}] Fetch Count [ {Done}/{All} ] => all fetching done",
            consumerChannelNumber, _queueName, consumerPrefetchSemaphore.CurrentCount, prefetchCount);

        consumerPrefetchSemaphore.Dispose();
        _consumerChannel?.Dispose();

        _logger.LogInformation("Consumer channel [{ChannelNo}] queue [{QueueName}] terminated", consumerChannelNumber, _queueName);
    }

    public void StartBasicConsume()
    {
        _logger.LogTrace("Creating RabbitMQ consumer channel...");

        if (_consumerChannel == null)
        {
            _logger.LogError("Could not start basic consume because consumer channel is null.");
            return;
        }

        _logger.LogTrace("Starting RabbitMQ basic consume...");
        _consumerChannel.BasicQos(0, prefetchCount, false);
        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.Received += ConsumerReceived;
        _consumerChannel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
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

                (sender as AsyncEventingBasicConsumer).Model.BasicAck(eventArgs.DeliveryTag, multiple: false);
                isAcknowledged = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing the following message: {Message}.", message);
            }
            finally
            {
                if (!isAcknowledged)
                {
                    TryEnqueueMessageAgainAsync((sender as AsyncEventingBasicConsumer).Model, eventArgs).GetAwaiter().GetResult();
                }

                consumerPrefetchSemaphore.Release();
            }
        });
    }

    private async Task TryEnqueueMessageAgainAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            _logger.LogWarning("Adding message to queue again with {Time} seconds delay...", $"{_subscribeRetryTime.TotalSeconds:n1}");

            await Task.Delay(_subscribeRetryTime);
            consumerChannel.BasicNack(eventArgs.DeliveryTag, false, true);

            _logger.LogTrace("Message added to queue again.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Could not enqueue message again: {Error}.", ex.Message);
        }
    }

    private void ProcessEvent(string eventName, string message)
    {
        _logger.LogTrace("Processing RabbitMQ event: {EventName}...", eventName);

        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogTrace("There are no subscriptions for this event.");
            return;
        }

        var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);
        // AbcEvent => AbcEventLogHandler, AbcEventMailHandler etc. Multiple subscription can be for one Event
        foreach (var subscription in subscriptions)
        {
            using var scope = _serviceProvider.CreateScope(); // because handler type scoped service
            var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
            if (handler == null)
            {
                _logger.LogWarning("There are no handlers for the following event: {EventName}", eventName);
                continue;
            }

            var eventType = _subscriptionsManager.GetEventTypeByName(eventName);

            var @event = JsonSerializer.Deserialize(message, eventType);
            var eventHandlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
            ((Task)eventHandlerType.GetMethod(nameof(IEventHandler<Event>.HandleAsync)).Invoke(handler, new object[] { @event })).GetAwaiter().GetResult();
        }

        _logger.LogTrace("Processed event {EventName}.", eventName);
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _logger.LogTrace("Creating RabbitMQ consumer channel...");

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _exchangeName, type: "direct");

        return channel;
    }
}