using Base.Core;
using Base.EventBus.Kafka;
using Confluent.Kafka;
using Shared;

namespace Kafka.Consumer.Consumers;

public class EmailKafkaConsumer : KafkaConsumerBase<EmailMessageIntegrationEvent>
{
    public EmailKafkaConsumer() : base("localhost:9092", "consumer-test", "emailmessage-topic")
    {
    }

    public EmailKafkaConsumer(string? consumerSuffix = "group") : base("localhost:9092", consumerSuffix, "emailmessage-topic")
    {
    }

    public void OnMessageDelivered(EmailMessageIntegrationEvent message)
    {
        ConsoleWriter.Info($"To: {message.To} \nContent: {message.Content} \nSubject: {message.Subject}");
        //todo email send business logic

        throw new Exception("exception:test error");
    }

    public override void OnErrorOccured(Error error)
    {
        ConsoleWriter.Info(new[] { "AAA", "BBB" });
        ConsoleWriter.Error("OnErrorOccured: {0}", error);

        //todo onerror business
    }
}