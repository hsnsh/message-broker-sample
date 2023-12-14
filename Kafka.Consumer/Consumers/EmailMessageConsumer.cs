using Base.Core;
using Base.EventBus.Kafka;
using Confluent.Kafka;
using Shared;

namespace Kafka.Consumer.Consumers;

public class EmailMessageConsumer : MessageConsumerBase<EmailMessage>
{
    public EmailMessageConsumer() : base("emailmessage-topic")
    {
    }

    public EmailMessageConsumer(string? consumerSuffix = "group") : base("emailmessage-topic", consumerSuffix)
    {
    }

    public override void OnMessageDelivered(EmailMessage message)
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