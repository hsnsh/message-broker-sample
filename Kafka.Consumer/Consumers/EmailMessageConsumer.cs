using System;
using Confluent.Kafka;
using Kafka.Message;
using Kafka.Message.Tools;

namespace Kafka.Consumer.Consumers
{
    public class EmailMessageConsumer : MessageConsumerBase<EmailMessage>
    {
        public EmailMessageConsumer() : base("emailmessage-topic")
        {
        }

        public override void OnMessageDelivered(EmailMessage message)
        {
            ConsoleWriter.Info($"To: {message.To} \nContent: {message.Content} \nSubject: {message.Subject}");
            //todo email send business logic

            throw new Exception("test error");
        }

        public override void OnErrorOccured(Error error)
        {
            ConsoleWriter.Info(new[] { "AAA", "BBB" });
            ConsoleWriter.Error("Error: {0}", error);

            //todo onerror business
        }
    }
}