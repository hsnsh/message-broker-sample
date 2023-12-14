#nullable enable
using Base.EventBus.Kafka.Converters;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Base.EventBus.Kafka;

public class EventBusKafka : IEventBus, IDisposable
{
    private readonly ILogger<EventBusKafka> _logger;
    private readonly IProducer<string, string> _producer;
    private readonly JsonSerializerSettings _options = DefaultJsonOptions.Get();

    public EventBusKafka(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EventBusKafka>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        var config = new ProducerConfig { BootstrapServers = "kafka:9092" };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler(ErrorHandler)
            .Build();
    }

    public async Task Publish(IntegrationEvent @event)
    {
        // if (!_persistentConnection.IsConnected)
        // {
        //     _persistentConnection.TryConnect();
        // }
        //
        // var policy = Policy.Handle<BrokerUnreachableException>()
        //     .Or<SocketException>()
        //     .WaitAndRetry(_eventBusConfig.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
        //     {
        //         _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
        //     });

        var eventTopicName = @event.GetType().Name;
        if (eventTopicName.EndsWith("IntegrationEvent"))
        {
            eventTopicName = eventTopicName[..^"IntegrationEvent".Length];
        }

        _logger.LogTrace("Creating MessageBroker channel to publish event: {EventId} ({EventName})", @event.Id, eventTopicName);

        //https://stackoverflow.com/a/29515696
        //If you require that messages with the same key (for instance, a unique id) are always seen in the
        //correct order, attaching a key to messages will ensure messages with the
        //same key always go to the same partition. TL;DR If you dont give key, it will use round robin
        try
        {
            var deliveryReport = await _producer.ProduceAsync(eventTopicName, new Message<string, string>
            {
                Key = null,
                Value = JsonConvert.SerializeObject(@event, _options)
            });

            if (deliveryReport.TopicPartitionOffset != null)
            {
                _logger.LogInformation("Completed MessageBroker channel to publish event: {EventId} ({EventName})", @event.Id, eventTopicName);
            }
            else
            {
                _logger.LogError("Failed MessageBroker channel to publish event: {EventId} ({EventName})", @event.Id, eventTopicName);
            }
        }
        catch (ProduceException<string, string> produceException)
        {
            _logger.LogError("Failed MessageBroker channel to publish event: {EventId} ({EventName}) , {Error}", @event.Id, eventTopicName, produceException.Message);
        }
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        // var eventName = typeof(T).Name;
        // eventName = TrimEventName(eventName);
        //
        // if (!_subsManager.HasSubscriptionsForEvent(eventName))
        // {
        //     if (!_persistentConnection.IsConnected)
        //     {
        //         _persistentConnection.TryConnect();
        //     }
        //
        //     _consumerChannel.QueueDeclare(queue: GetSubName(eventName), //Ensure queue exists while consuming
        //         durable: true,
        //         exclusive: false,
        //         autoDelete: false,
        //         arguments: null);
        //
        //     _consumerChannel.QueueBind(queue: GetSubName(eventName),
        //         exchange: _eventBusConfig.DefaultTopicName,
        //         routingKey: eventName);
        // }
        //
        // _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
        //
        // _subsManager.AddSubscription<T, TH>();
        // StartBasicConsume(eventName);
    }

    public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        // var eventName = _subsManager.GetEventKey<T>();
        // eventName = TrimEventName(eventName);
        //
        // _logger.LogInformation("Unsubscribing from event {EventName}", eventName);
        //
        // _subsManager.RemoveSubscription<T, TH>();
    }

    public void Dispose()
    {
        if (_producer != null)
        {
            _producer.Dispose();
        }

        // if (_consumerChannel != null)
        // {
        //     _consumerChannel.Dispose();
        // }
        //
        // _subsManager.Clear();
    }

    private void ErrorHandler(IProducer<string, string> arg1, Error arg2)
    {
        // _logHelper.FrameworkInformationLog(new FrameworkInformationLog()
        // {
        //     Description = "ErrorHandler for producer invoked.",
        //     Reason = FrameworkLogReason.ProducerWriteMessageFailed.ToString("G"),
        //     Exception =
        //         $"Exception occured: {arg2.Reason}. Code: {arg2.Code}, IsFatal: {arg2.IsFatal}, IsError: {arg2.IsError}, IsBrokerError: {arg2.IsBrokerError}, IsLocalError: {arg2.IsLocalError}",
        //     RequestSource = "KAFKA",
        //     Hostname = Dns.GetHostName()
        // });
    }
}