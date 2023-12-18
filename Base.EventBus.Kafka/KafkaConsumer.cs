using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Base.EventBus.Kafka;

public sealed class KafkaConsumer
{
    private readonly ILogger _logger;
    private readonly ConsumerConfig _consumerConfig;
    private bool KeepConsuming { get; set; }

    public event EventHandler<object> OnMessageReceived;

    public KafkaConsumer(EventBusConfig eventBusConfig, ILogger logger)
    {
        _logger = logger;
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = eventBusConfig.EventBusConnectionString,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            GroupId = eventBusConfig.SubscriberClientAppName,

            // Read messages from start if no commit exists.
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        KeepConsuming = true;
    }

    public void StartReceivingMessages<TEvent>(string topicName, CancellationToken stoppingToken) where TEvent : IntegrationEvent
    {
        StartReceivingMessages(typeof(TEvent), topicName, stoppingToken);
    }

    public void StartReceivingMessages(Type eventType, string topicName, CancellationToken stoppingToken)
    {
        if (!eventType.IsAssignableTo(typeof(IntegrationEvent))) throw new TypeAccessException();

        using var consumer = new ConsumerBuilder<long, string>(_consumerConfig)
            .SetKeyDeserializer(Deserializers.Int64)
            .SetValueDeserializer(Deserializers.Utf8)
            .SetLogHandler((_, message) =>
            {
                switch (message.Level)
                {
                    case SyslogLevel.Emergency | SyslogLevel.Alert | SyslogLevel.Critical | SyslogLevel.Error:
                    {
                        _logger.LogError("Kafka Consumer [ {TopicName} ] => {Facility}, Message: {Message}", topicName, message.Facility, message.Message);
                        break;
                    }
                    case SyslogLevel.Warning | SyslogLevel.Notice | SyslogLevel.Debug:
                    {
                        _logger.LogDebug("Kafka Consumer [ {TopicName} ] => {Facility}, Message: {Message}", topicName, message.Facility, message.Message);
                        break;
                    }
                    default:
                    {
                        _logger.LogInformation("Kafka Consumer [ {TopicName} ] => {Facility}, Message: {Message}", topicName, message.Facility, message.Message);
                        break;
                    }
                }
            }).SetErrorHandler((_, e) =>
            {
                _logger.LogError("Error: {Reason}. Is Fatal: {IsFatal}", e.Reason, e.IsFatal);
                KeepConsuming = !e.IsFatal;
            })
            .Build();

        try
        {
            consumer.Subscribe(topicName);
            _logger.LogInformation("Kafka Consumer [ {TopicName} ] => Subscribed", topicName);

            while (KeepConsuming && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    // var result = consumer.Consume(TimeSpan.FromMilliseconds(_consumerConfig.MaxPollIntervalMs - 1000 ?? 250000));
                    var message = result?.Message?.Value;
                    if (message == null)
                    {
                        _logger.LogDebug("Kafka Consumer [ {TopicName} ] => Loop [ {Time} ]", topicName, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
                        continue;
                    }

                    _logger.LogDebug("{ConsumerIdentifier} Received: {Key}:{Message} from partition: {Partition}", _consumerConfig.GroupId, result.Message.Key, message, result.Partition.Value);

                    consumer.Commit(result);
                    consumer.StoreOffset(result);

                    var @event = JsonConvert.DeserializeObject(message, eventType);

                    OnMessageReceived?.Invoke(this, @event);
                }
                catch (ConsumeException ce)
                {
                    if (ce.Error.IsFatal) throw;
                    _logger.LogWarning("Kafka Consumer [ {TopicName} ] => loop [ {Time} ] | Error: {ConsumeError}", topicName, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"), ce.Message);
                }

                // loop wait period
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
            }
        }
        catch (OperationCanceledException oe)
        {
            _logger.LogWarning("Kafka Consumer [ {TopicName} ] => Closing... : {CancelledMessage}", topicName, oe.Message);
        }
        catch (Exception e)
        {
            _logger.LogError("Kafka Consumer [ {TopicName} ] => FatalError : {FatalError}", topicName, e.Message);
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka Consumer [ {TopicName} ] => Closed", topicName);
        }
    }
}