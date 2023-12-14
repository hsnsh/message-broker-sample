using System;
using Confluent.Kafka;
using Kafka.Message.Tools;
using Newtonsoft.Json;

namespace Kafka.Consumer.Consumers
{
    public abstract class MessageConsumerBase<IMessage>
    {
        private readonly string _consumerSuffix;
        private readonly string _topic;
        private bool KeepConsuming { get; set; }

        protected MessageConsumerBase(string topic, string? consumerSuffix = "group")
        {
            _consumerSuffix = consumerSuffix ?? "group";
            _topic = topic;
            KeepConsuming = true;
        }

        public void StartConsuming()
        {
            var conf = new ConsumerConfig
            {
                GroupId = $"emailmessage-consumer-{_consumerSuffix}",
                BootstrapServers = "kafka:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var consumer = new ConsumerBuilder<Ignore, string>(conf)
                .SetErrorHandler(ErrorHandler)
                .Build();


            consumer.Subscribe(_topic);

            while (KeepConsuming)
            {
                try
                {
                    ConsoleWriter.Info("Wait consume...");
                    var consumedTextMessage = consumer.Consume();
                    ConsoleWriter.Info($"Consumed message '{consumedTextMessage.Value}' Topic: '{consumedTextMessage.Topic}'.");

                    var message = JsonConvert.DeserializeObject<IMessage>(consumedTextMessage.Value);

                    OnMessageDelivered(message);
                }
                catch (ConsumeException ce)
                {
                    OnErrorOccured(ce.Error);
                }
                catch (Exception ex)
                {
                    OnErrorOccured(new Error(ErrorCode.Unknown, ex.Message));
                }
            }

            // Ensure the consumer leaves the group cleanly and final offsets are committed.
            consumer.Close();
        }

        private void ErrorHandler(IConsumer<Ignore, string> arg1, Error arg2)
        {
            KeepConsuming = !arg2.IsFatal;
            // _logHelper.FrameworkDebugLog(new FrameworkDebugLog()
            // {
            //     Topic = arg1.Subscription.First(),
            //     Description = "ErrorHandler for consumer invoked.",
            //     Reason = FrameworkLogReason.ConsumerErrorHandlerInvoked.ToString("G"),
            //     Exception =
            //         $"Exception occured: {arg2.Reason}. Code: {arg2.Code}, IsFatal: {arg2.IsFatal}, IsError: {arg2.IsError}, IsBrokerError: {arg2.IsBrokerError}, IsLocalError: {arg2.IsLocalError}",
            //     RequestSource = "KAFKA",
            //     Message = null,
            //     ResponseMessage = null
            // });
        }

        public abstract void OnMessageDelivered(IMessage message);

        public abstract void OnErrorOccured(Error error);
    }
}