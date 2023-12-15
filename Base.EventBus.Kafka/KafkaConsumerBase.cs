using Base.Core;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace Base.EventBus.Kafka;

public class KafkaConsumerBase<TEvent> where TEvent : IIntegrationEvent
{
    public event EventHandler<IIntegrationEvent> OnMessageDelivered;

    private readonly ConsumerConfig _consumerConfig;
    private readonly string _subscribeTopicName;
    private bool KeepConsuming { get; set; }

    public KafkaConsumerBase(string bootstrapServer, string consumerGroupId, string subscribeTopicName)
    {
        _subscribeTopicName = subscribeTopicName;
        _consumerConfig = new ConsumerConfig
        {
            GroupId = consumerGroupId,
            BootstrapServers = bootstrapServer,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        KeepConsuming = true;
    }

    public void StartConsuming()
    {
        var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig)
            .SetErrorHandler(ErrorHandler)
            .Build();

        consumer.Subscribe(_subscribeTopicName);

        while (KeepConsuming)
        {
            try
            {
                ConsoleWriter.Info($"Wait consume...({_subscribeTopicName})");
                var consumedTextMessage = consumer.Consume();
                ConsoleWriter.Info($"Topic: '{consumedTextMessage.Topic}'. Consumed message '{consumedTextMessage.Value}'");

                var message = JsonConvert.DeserializeObject<TEvent>(consumedTextMessage.Value);

                OnMessageDelivered(this, message);
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

    public virtual void OnErrorOccured(Error error)
    {
    }
}