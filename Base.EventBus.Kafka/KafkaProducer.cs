using System.Net;
using Base.EventBus.Kafka.Converters;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Base.EventBus.Kafka;

public sealed class KafkaProducer
{
    private readonly ILogger _logger;
    private readonly ProducerConfig _producerConfig;
    private readonly JsonSerializerSettings _options = DefaultJsonOptions.Get();

    public KafkaProducer(string bootstrapServer, ILogger logger)
    {
        _logger = logger;
        _producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServer ?? throw new ArgumentNullException(nameof(bootstrapServer)),
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
    }

    public async Task StartSendingMessages<TEvent>(string topicName, TEvent @event) where TEvent : IntegrationEvent
    {
        using var producer = new ProducerBuilder<long, string>(_producerConfig)
            .SetKeySerializer(Serializers.Int64)
            .SetValueSerializer(Serializers.Utf8)
            .SetLogHandler((_, message) => _logger.LogInformation("Facility: {Facility}-{Level} Message: {Message}", message.Facility, message.Level, message.Message))
            .SetErrorHandler((_, e) => _logger.LogError("Error: {Reason}. Is Fatal: {IsFatal}", e.Reason, e.IsFatal))
            .Build();

        try
        {
            _logger.LogInformation("Kafka Producer [ {TopicName} ] => EventId [ {EventId} ] started", topicName, @event.Id.ToString());

            var message = JsonConvert.SerializeObject(@event, _options);

            var deliveryReport = await producer.ProduceAsync(topicName,
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
            _logger.LogError("Permanent error: {Message} for message (value: \'{DeliveryResultValue}\')", e.Message, e.DeliveryResult.Value);
        }
        finally
        {
            _logger.LogInformation("Kafka Producer [ {TopicName} ] => EventId [ {EventId} ] completed", topicName, @event.Id.ToString());
        }
    }
}