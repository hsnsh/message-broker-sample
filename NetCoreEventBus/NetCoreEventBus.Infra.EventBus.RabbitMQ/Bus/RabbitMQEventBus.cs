﻿using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetCoreEventBus.Infra.EventBus.Bus;
using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Infra.EventBus.RabbitMQ.Connection;
using NetCoreEventBus.Infra.EventBus.Subscriptions;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace NetCoreEventBus.Infra.EventBus.RabbitMQ.Bus;

public class RabbitMQEventBus : IEventBus
{
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly int _publishRetryCount = 5;
    private readonly TimeSpan _subscribeRetryTime = TimeSpan.FromSeconds(5);

    private readonly IPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger<RabbitMQEventBus> _logger;

    // private IModel _consumerChannel;
    private readonly ConcurrentDictionary<string, IModel> consumerChannels;
    private readonly List<Task> _consumerTasks;

    private const int prefetchCount = 2;
    private readonly SemaphoreSlim consumerPrefetchSemaphore;

    public RabbitMQEventBus(
        IPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger,
        string brokerName,
        string queueName)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exchangeName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

        _subscriptionsManager.OnEventRemoved += SubscriptionManager_OnEventRemoved;
        // _persistentConnection.OnReconnectedAfterConnectionFailure += PersistentConnection_OnReconnectedAfterConnectionFailure;

        consumerChannels = new ConcurrentDictionary<string, IModel>();
        _consumerTasks = new List<Task>();
        consumerPrefetchSemaphore = new SemaphoreSlim(prefetchCount);
    }

    public void Publish<TEvent>(TEvent @event)
        where TEvent : Event
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_publishRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timeSpan) =>
            {
                _logger.LogWarning(exception, "Could not publish event #{EventId} after {Timeout} seconds: {ExceptionMessage}.", @event.Id, $"{timeSpan.TotalSeconds:n1}", exception.Message);
            });

        var eventName = @event.GetType().Name;

        _logger.LogTrace("Creating RabbitMQ channel to publish event #{EventId} ({EventName})...", @event.Id, eventName);

        using (var channel = _persistentConnection.CreateModel())
        {
            _logger.LogTrace("Declaring RabbitMQ exchange to publish event #{EventId}...", @event.Id);

            channel.ExchangeDeclare(exchange: _exchangeName, type: "direct");

            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = (byte)DeliveryMode.Persistent;

                _logger.LogTrace("Publishing event to RabbitMQ with ID #{EventId}...", @event.Id);

                channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);

                _logger.LogTrace("Published event with ID #{EventId}.", @event.Id);
            });
        }
    }

    public void Subscribe<TEvent, TEventHandler>()
        where TEvent : Event
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventIdentifier<TEvent>();
        var eventHandlerName = typeof(TEventHandler).Name;

        AddQueueBindForEventSubscription(eventName);

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}...", eventName, eventHandlerName);

        _subscriptionsManager.AddSubscription<TEvent, TEventHandler>();

        for (int i = 0; i < 1; i++)
        {
            StartBasicConsume();
        }

        _logger.LogInformation("Subscribed to event {EventName} with {EvenHandler}.", eventName, eventHandlerName);
    }

    private void AddQueueBindForEventSubscription(string eventName)
    {
        var containsKey = _subscriptionsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey)
        {
            return;
        }

        using (var channel = CreateConsumerChannel())
        {
            channel.QueueDeclare
            (
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: eventName);
        }
    }

    public void Unsubscribe<TEvent, TEventHandler>()
        where TEvent : Event
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventIdentifier<TEvent>();

        _logger.LogInformation("Unsubscribing from event {EventName}...", eventName);

        _subscriptionsManager.RemoveSubscription<TEvent, TEventHandler>();

        _logger.LogInformation("Unsubscribed from event {EventName}.", eventName);
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

    private void StartBasicConsume()
    {
        _logger.LogTrace("Creating RabbitMQ consumer channel...");

        var channel = CreateConsumerChannel();
        if (channel == null)
        {
            _logger.LogError("Could not start basic consume because consumer channel is null.");
            return;
        }

        _logger.LogTrace("Starting RabbitMQ basic consume...");
        channel.BasicQos(0, prefetchCount, false);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += ConsumerReceived;
        channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        consumerChannels.TryAdd(_queueName + "_" + channel.ChannelNumber, channel);
    }

    private async Task ConsumerReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        await consumerPrefetchSemaphore.WaitAsync();

        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        _consumerTasks.Add(Task.Run(() =>
        {
            bool isAcknowledged = false;

            _logger.LogInformation($"[ConsumerTag: {(sender as AsyncEventingBasicConsumer).ConsumerTags.FirstOrDefault() ?? string.Empty}]");
            try
            {
                //Console.WriteLine($"[ConsumerTag: {(ch as EventingBasicConsumer).ConsumerTag}]  [{DateTime.Now}]  [Message: {message}]  [Thread Name: {Thread.CurrentThread.Name}]  [Thread Number: {Thread.CurrentThread.ManagedThreadId}]");

                ProcessEvent(eventName, message).GetAwaiter().GetResult();

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
        }));
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

    private async Task ProcessEvent(string eventName, string message)
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
            await Task.Yield();
            await (Task)eventHandlerType.GetMethod(nameof(IEventHandler<Event>.HandleAsync)).Invoke(handler, new object[] { @event });
        }

        _logger.LogTrace("Processed event {EventName}.", eventName);
    }

    private void SubscriptionManager_OnEventRemoved(object sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using (var channel = _persistentConnection.CreateModel())
        {
            channel.QueueUnbind(queue: _queueName, exchange: _exchangeName, routingKey: eventName);

            if (_subscriptionsManager.IsEmpty)
            {
                // TODO:  _consumerChannel.Close();
            }
        }
    }

    // private void PersistentConnection_OnReconnectedAfterConnectionFailure(object sender, EventArgs e)
    // {
    //     DoCreateConsumerChannel();
    //     RecreateSubscriptions();
    // }
    //
    // private void DoCreateConsumerChannel()
    // {
    //     _consumerChannel.Dispose();
    //     _consumerChannel = CreateConsumerChannel();
    //     StartBasicConsume();
    // }

    // private void RecreateSubscriptions()
    // {
    //     var subscriptions = _subscriptionsManager.GetAllSubscriptions();
    //     _subscriptionsManager.Clear();
    //
    //     Type eventBusType = this.GetType();
    //     MethodInfo genericSubscribe;
    //
    //     foreach (var entry in subscriptions)
    //     {
    //         foreach (var subscription in entry.Value)
    //         {
    //             genericSubscribe = eventBusType.GetMethod("Subscribe").MakeGenericMethod(subscription.EventType, subscription.HandlerType);
    //             genericSubscribe.Invoke(this, null);
    //         }
    //     }
    // }
}