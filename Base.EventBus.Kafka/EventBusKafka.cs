#nullable enable
using System.Net;
using Base.EventBus.Kafka.Converters;
using Base.EventBus.SubManagers;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Base.EventBus.Kafka;

public class EventBusKafka : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusKafka> _logger;
    private readonly EventBusConfig _eventBusConfig;
    private readonly string _bootstrapServer;

    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly JsonSerializerSettings _options = DefaultJsonOptions.Get();

    public EventBusKafka(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, EventBusConfig eventBusConfig, string bootstrapServer)
    {
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<EventBusKafka>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _eventBusConfig = eventBusConfig;
        _bootstrapServer = bootstrapServer ?? throw new ArgumentNullException(nameof(bootstrapServer));

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    public async Task Publish(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServer,
            EnableDeliveryReports = true,
            ClientId = Dns.GetHostName(),
            Debug = "msg",

            // retry settings:
            // Receive acknowledgement from all sync replicas
            Acks = Acks.All,
            // Number of times to retry before giving up
            MessageSendMaxRetries = 3,
            // Duration to retry before next attempt
            RetryBackoffMs = 1000,
            // Set to true if you don't want to reorder messages on retry
            EnableIdempotence = true
        };

        using var producer = new ProducerBuilder<long, string>(producerConfig)
            .SetKeySerializer(Serializers.Int64)
            .SetValueSerializer(Serializers.Utf8)
            .SetLogHandler((_, message) => _logger.LogInformation("Facility: {Facility}-{Level} Message: {Message}", message.Facility, message.Level, message.Message))
            .SetErrorHandler((_, e) => _logger.LogError("Error: {Reason}. Is Fatal: {IsFatal}", e.Reason, e.IsFatal))
            .Build();
        try
        {
            _logger.LogInformation("Kafka Producer loop started...");

            var message = JsonConvert.SerializeObject(@event, _options);

            var deliveryReport = await producer.ProduceAsync(eventName,
                new Message<long, string>
                {
                    Key = DateTime.UtcNow.Ticks,
                    Value = message
                });

            producer.Flush(new TimeSpan(0, 0, 10));
            _logger.LogInformation("Message sent (value: \'{Message}\'). Delivery status: {DeliveryReportStatus}", message, deliveryReport.Status);

            if (deliveryReport.Status != PersistenceStatus.Persisted)
            {
                // delivery might have failed after retries. This message requires manual processing.
                _logger.LogError("Message not ack\'d by all brokers (value: \'{Message}\'). Delivery status: {DeliveryReportStatus}", message, deliveryReport.Status);
            }

            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
        catch (ProduceException<long, string> e)
        {
            // Log this message for manual processing.
            _logger.LogInformation("Permanent error: {Message} for message (value: \'{DeliveryResultValue}\')", e.Message, e.DeliveryResult.Value);
            _logger.LogInformation("Exiting Kafka Producer...");
        }
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        eventName = TrimEventName(eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            // if (!_persistentConnection.IsConnected)
            // {
            //     _persistentConnection.TryConnect();
            // }
            //
            // _consumerChannel.QueueDeclare(queue: GetSubName(eventName), //Ensure queue exists while consuming
            //     durable: true,
            //     exclusive: false,
            //     autoDelete: false,
            //     arguments: null);
            //
            // _consumerChannel.QueueBind(queue: GetSubName(eventName),
            //     exchange: _eventBusConfig.DefaultTopicName,
            //     routingKey: eventName);
        }

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

        _subsManager.AddSubscription<T, TH>();
        StartBasicConsume<T>(eventName);
    }

    public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        eventName = TrimEventName(eventName);

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void Dispose()
    {
        // if (_consumerChannel != null)
        // {
        //     _consumerChannel.Dispose();
        // }

        _subsManager.Clear();
    }

    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        // if (!_persistentConnection.IsConnected)
        // {
        //     _persistentConnection.TryConnect();
        // }
        //
        // using (var channel = _persistentConnection.CreateModel())
        // {
        //     channel.QueueUnbind(queue: GetSubName(eventName),
        //         exchange: _eventBusConfig.DefaultTopicName,
        //         routingKey: TrimEventName(eventName));
        //
        //     if (_subsManager.IsEmpty)
        //     {
        //         _consumerChannel.Close();
        //     }
        // }
    }

    private void StartBasicConsume<T>(string eventName) where T : IntegrationEvent
    {
        _logger.LogTrace("Starting MessageBroker basic consume");

        var consumer = new KafkaConsumerBase<T>(_bootstrapServer, _eventBusConfig.SubscriberClientAppName, eventName, _logger);
        consumer.OnMessageDelivered += OnMessageDelivered;
        consumer.StartConsuming();
    }

    private async void OnMessageDelivered(object? sender, IntegrationEvent message)
    {
        var eventName = message.GetType().Name;
        eventName = TrimEventName(eventName);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var subscriptions = _subsManager.GetHandlersForEvent(eventName);

            using var scope = _serviceProvider.CreateScope();
            foreach (var subscription in subscriptions)
            {
                var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    _logger.LogWarning("{ConsumerGroupId} consumed message [ {Topic} ] => No event handler for event", _eventBusConfig.SubscriberClientAppName, eventName);
                    continue;
                }

                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(message.GetType());
                await (Task)concreteType.GetMethod("Handle")?.Invoke(handler, new object[] { message })!;
            }
        }
        else
        {
            _logger.LogWarning("{ConsumerGroupId} consumed message [ {Topic} ] => No subscription for event", _eventBusConfig.SubscriberClientAppName, eventName);
        }
    }

    private string TrimEventName(string eventName)
    {
        if (_eventBusConfig.DeleteEventPrefix && eventName.StartsWith(_eventBusConfig.EventNamePrefix))
        {
            eventName = eventName.Substring(_eventBusConfig.EventNamePrefix.Length);
        }

        if (_eventBusConfig.DeleteEventSuffix && eventName.EndsWith(_eventBusConfig.EventNameSuffix))
        {
            eventName = eventName.Substring(0, eventName.Length - _eventBusConfig.EventNameSuffix.Length);
        }

        return eventName;
    }
}