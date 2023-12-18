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

    public KafkaProducer(EventBusConfig eventBusConfig, ILogger logger)
    {
        _logger = logger;
        _producerConfig = new ProducerConfig
        {
            BootstrapServers = eventBusConfig?.EventBusConnectionString ?? throw new ArgumentNullException(nameof(eventBusConfig.EventBusConnectionString)),
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
            .SetLogHandler((_, message) =>
            {
                switch (message.Level)
                {
                    case SyslogLevel.Emergency | SyslogLevel.Alert | SyslogLevel.Critical | SyslogLevel.Error:
                    {
                        _logger.LogError("Kafka Producer [ {TopicName} ] => {Facility}, Message: {Message}", topicName, message.Facility, message.Message);
                        break;
                    }
                    case SyslogLevel.Warning | SyslogLevel.Notice | SyslogLevel.Debug:
                    {
                        _logger.LogDebug("Kafka Producer [ {TopicName} ] => {Facility}, Message: {Message}", topicName, message.Facility, message.Message);
                        break;
                    }
                    default:
                    {
                        _logger.LogInformation("Kafka Producer [ {TopicName} ] => {Facility}, Message: {Message}", topicName, message.Facility, message.Message);
                        break;
                    }
                }
            })
            .SetErrorHandler((_, e) => _logger.LogError("Error: {Reason}. Is Fatal: {IsFatal}", e.Reason, e.IsFatal))
            .Build();

        try
        {
            _logger.LogInformation("Kafka Producer [ {TopicName} ] => EventId [ {EventId} ] STARTED", topicName, @event.Id.ToString());

            var message = JsonConvert.SerializeObject(@event, _options);

            var deliveryReport = await producer.ProduceAsync(topicName,
                new Message<long, string>
                {
                    Key = DateTime.UtcNow.Ticks,
                    Value = message
                });

            producer.Flush(new TimeSpan(0, 0, 10));
            if (deliveryReport.Status != PersistenceStatus.Persisted)
            {
                // delivery might have failed after retries. This message requires manual processing.
                _logger.LogError("Message not ack\'d by all brokers (value: \'{Message}\'). Delivery status: {DeliveryReportStatus}", message, deliveryReport.Status);
            }
            else
            {
                _logger.LogDebug("Message sent (value: \'{Message}\'). Delivery status: {DeliveryReportStatus}", message, deliveryReport.Status);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            _logger.LogInformation("Kafka Producer [ {TopicName} ] => EventId [ {EventId} ] COMPLETED", topicName, @event.Id.ToString());
        }
        catch (ProduceException<long, string> e)
        {
            // Log this message for manual processing.
            _logger.LogError("Kafka Producer [ {TopicName} ] => EventId [ {EventId} ] Error: {ProduceError} for message (value: \'{DeliveryResultValue}\')", topicName, @event.Id.ToString(), e.Message, e.DeliveryResult.Value);
        }
    }
}