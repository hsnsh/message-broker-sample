using Base.EventBus.Kafka.Converters;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Base.EventBus.Kafka;

public sealed class KafkaConsumer
{
    private readonly ILogger _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly JsonSerializerSettings _options = DefaultJsonOptions.Get();
    private bool KeepConsuming { get; set; }

    public event EventHandler<IntegrationEvent> OnMessageReceived;

    public KafkaConsumer(string bootstrapServer, string consumerGroupId, ILogger logger)
    {
        _logger = logger;
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServer ?? throw new ArgumentNullException(nameof(bootstrapServer)),
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            GroupId = consumerGroupId ?? throw new ArgumentNullException(nameof(consumerGroupId)),

            // Read messages from start if no commit exists.
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        KeepConsuming = true;
    }

    public void StartReceivingMessages<TEvent>(string topicName, CancellationToken cancellationToken) where TEvent : IntegrationEvent
    {
        using var consumer = new ConsumerBuilder<long, string>(_consumerConfig)
            .SetKeyDeserializer(Deserializers.Int64)
            .SetValueDeserializer(Deserializers.Utf8)
            .SetLogHandler((_, message) => _logger.LogInformation("Facility: {Facility}-{Level} Message: {Message}", message.Facility, message.Level, message.Message))
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError("Error: {Reason}. Is Fatal: {IsFatal}", e.Reason, e.IsFatal);
                KeepConsuming = !e.IsFatal;
            })
            .Build();

        try
        {
            consumer.Subscribe(topicName);
            _logger.LogInformation("Kafka Consumer [ {TopicName} ] loop started...", topicName);

            while (KeepConsuming && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Kafka Consumer [ {TopicName} ] RE-LOOP...", topicName);

                // var result = consumer.Consume(1000);
                // var message = result?.Message?.Value;
                // if (message == null)
                // {
                //     continue;
                // }
                //
                // _logger.LogInformation("{ConsumerGroupId} Received: {Key}:{Message} from partition: {Partition}", _consumerConfig.GroupId, result.Message.Key, message, result.Partition.Value);
                //
                // consumer.Commit(result);
                // consumer.StoreOffset(result);
                //
                // var @event = JsonConvert.DeserializeObject<TEvent>(message);
                //
                // OnMessageReceived(this, @event);

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
        catch (KafkaException e)
        {
            _logger.LogError("Consume error: {Message}", e.Message);
            _logger.LogInformation("Exiting Kafka Consumer...");
        }
        finally
        {
            consumer.Close();
        }
    }
}