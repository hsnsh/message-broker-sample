using Microsoft.Extensions.Logging;
using NetCoreEventBus.Infra.EventBus.Bus;
using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Infra.EventBus.RabbitMQ.Connection;
using NetCoreEventBus.Infra.EventBus.Subscriptions;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NetCoreEventBus.Infra.EventBus.RabbitMQ.Bus;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly ILogger<RabbitMQEventBus> _logger;

    private readonly int _publishRetryCount = 5;
    private readonly List<RabbitMqConsumer> _consumers;
    private bool _disposed;
    private bool _publishing;

    public RabbitMQEventBus(
        IServiceScopeFactory serviceScopeFactory,
        IRabbitMqPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        IOptions<RabbitMqEventBusConfig> eventBusSettings,
        ILogger<RabbitMQEventBus> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
        _rabbitMqEventBusConfig = eventBusSettings.Value;
        _logger = logger;

        _subscriptionsManager.EventNameGetter = TrimEventName;
        _subscriptionsManager.OnEventRemoved += SubscriptionManager_OnEventRemoved;
        // _persistentConnection.OnReconnectedAfterConnectionFailure += PersistentConnection_OnReconnectedAfterConnectionFailure;

        _consumers = new List<RabbitMqConsumer>();
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : Event
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _publishing = true;

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _publishing = false;
                _logger.LogError("RabbitMQ | Could not publish event message : {Event} after {Timeout}s ({ExceptionMessage})", @event, $"{time.TotalSeconds:n1}", ex.Message);
            });

        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        _logger.LogTrace("Creating RabbitMQ channel to publish event #{EventId} ({EventName})...", @event.Id, eventName);

        using (var channel = _persistentConnection.CreateModel())
        {
            _logger.LogTrace("Declaring RabbitMQ exchange to publish event #{EventId}...", @event.Id);

            channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = (byte)DeliveryMode.Persistent;

                _logger.LogTrace("Publishing event to RabbitMQ with ID #{EventId}...", @event.Id);

                channel.BasicPublish(
                    exchange: _rabbitMqEventBusConfig.ExchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);

                _logger.LogTrace("Published event with ID #{EventId}.", @event.Id);
            });
        }

        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        _publishing = false;
    }

    public void Subscribe<TEvent, TEventHandler>() where TEvent : Event where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventKey<TEvent>();

        var eventHandlerName = typeof(TEventHandler).Name;

        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _logger.LogTrace("Creating RabbitMQ consumer channel...");

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

                channel.QueueDeclare
                (
                    queue: GetConsumerQueueName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                channel.QueueBind(queue: GetConsumerQueueName(eventName), exchange: _rabbitMqEventBusConfig.ExchangeName, routingKey: eventName);
            }
        }

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}...", eventName, eventHandlerName);

        _subscriptionsManager.AddSubscription<TEvent, TEventHandler>();

        for (int i = 0; i < _rabbitMqEventBusConfig.ConsumerParallelThreadCount; i++)
        {
            var rabbitMqConsumer = new RabbitMqConsumer(_serviceScopeFactory, _persistentConnection, _subscriptionsManager, _rabbitMqEventBusConfig, _logger);
            rabbitMqConsumer.StartBasicConsume(eventName);

            _consumers.Add(rabbitMqConsumer);
        }

        _logger.LogInformation("Subscribed to event {EventName} with {EvenHandler}.", eventName, eventHandlerName);
    }

    public void Unsubscribe<TEvent, TEventHandler>()
        where TEvent : Event
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventKey<TEvent>();

        _logger.LogInformation("Unsubscribing from event {EventName}...", eventName);

        _subscriptionsManager.RemoveSubscription<TEvent, TEventHandler>();

        _logger.LogInformation("Unsubscribed from event {EventName}.", eventName);
    }

    private void SubscriptionManager_OnEventRemoved([CanBeNull] object sender, string eventName)
    {
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogInformation("Message Broker Bridge shutting down...");

        Task.WaitAll(_consumers.Select(consumer => Task.Run(consumer.Dispose)).ToArray());

        _subscriptionsManager.Clear();
        _consumers.Clear();

        if (_persistentConnection?.IsConnected == true)
        {
            _persistentConnection?.Dispose();
        }

        _logger.LogInformation("Message Broker Bridge terminated");
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