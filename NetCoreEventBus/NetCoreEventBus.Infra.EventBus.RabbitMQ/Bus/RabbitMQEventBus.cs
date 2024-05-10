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
using Microsoft.Extensions.DependencyInjection;

namespace NetCoreEventBus.Infra.EventBus.RabbitMQ.Bus;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly string _exchangeName;
    private readonly string _queueName;

    private readonly int _consumerMultiThreadChannelCount = 5;
    private readonly int _publishRetryCount = 5;
    private readonly List<RabbitMqConsumer> _consumers;
    private bool _disposed;

    public RabbitMQEventBus(
        IServiceScopeFactory serviceScopeFactory,
        IPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        ILogger<RabbitMQEventBus> logger,
        string brokerName,
        string queueName)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
        _logger = logger;
        _exchangeName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

        _subscriptionsManager.OnEventRemoved += SubscriptionManager_OnEventRemoved;
        // _persistentConnection.OnReconnectedAfterConnectionFailure += PersistentConnection_OnReconnectedAfterConnectionFailure;

        _consumers = new List<RabbitMqConsumer>();
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

        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _logger.LogTrace("Creating RabbitMQ consumer channel...");

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: _exchangeName, type: "direct");

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

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}...", eventName, eventHandlerName);

        _subscriptionsManager.AddSubscription<TEvent, TEventHandler>();

        for (int i = 0; i < _consumerMultiThreadChannelCount; i++)
        {
            var rabbitMqConsumer = new RabbitMqConsumer(_serviceScopeFactory, _persistentConnection, _subscriptionsManager, _logger, _queueName, _exchangeName);
            rabbitMqConsumer.StartBasicConsume();

            _consumers.Add(rabbitMqConsumer);
        }

        _logger.LogInformation("Subscribed to event {EventName} with {EvenHandler}.", eventName, eventHandlerName);
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogInformation("Message Broker Bridge shutting down...");

        Task.WaitAll(_consumers.Select(consumer => Task.Run(consumer.Dispose)).ToArray());
        _subscriptionsManager.Clear();

        _logger.LogInformation("Message Broker Bridge terminated");
    }
}