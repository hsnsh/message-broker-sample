using System;
using System.Threading.Tasks;
using Base.EventBus;
using Confluent.Kafka;
using Kafka.Message;
using Kafka.Producer.Converters;
using Newtonsoft.Json;

namespace Kafka.Producer
{
    public class MessageProducer : IMessageProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly JsonSerializerSettings _options = DefaultJsonOptions.Get();

        public MessageProducer()
        {
            var config = new ProducerConfig { BootstrapServers = "kafka:9092" };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler(ErrorHandler)
                .Build();
        }


        // public void Produce(string topic, IMessageBase message, string key = null)
        // {
        //     var textMessage = JsonConvert.SerializeObject(message);
        //
        //     producer.BeginProduce(topic, new Message<Null, string> { Value = textMessage }, OnDelivery);
        // }
        //
        // private void OnDelivery(DeliveryReportResult<Null, string> r)
        // {
        //     Console.WriteLine(!r.Error.IsError ? $"Delivered message to {r.TopicPartitionOffset}" : $"Delivery Error: {r.Error.Reason}");
        // }

        public bool Produce(IntegrationEvent message, string topic, string key = null)
        {
            //https://stackoverflow.com/a/29515696
            //If you require that messages with the same key (for instance, a unique id) are always seen in the
            //correct order, attaching a key to messages will ensure messages with the
            //same key always go to the same partition. TL;DR If you dont give key, it will use round robin
            try
            {
                var deliveryReport = _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = JsonConvert.SerializeObject(message, _options)
                }).Result;

                return deliveryReport.TopicPartitionOffset != null;
            }
            catch (ProduceException<string, string> produceException)
            {
                return false;
            }
        }

        public async Task<bool> ProduceAsync(IntegrationEvent message, string topic, string key = null)
        {
            //https://stackoverflow.com/a/29515696
            //If you require that messages with the same key (for instance, a unique id) are always seen in the
            //correct order, attaching a key to messages will ensure messages with the
            //same key always go to the same partition. TL;DR If you dont give key, it will use round robin
            try
            {
                var deliveryReport = await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = JsonConvert.SerializeObject(message, _options)
                });

                return deliveryReport.TopicPartitionOffset != null;
            }
            catch (ProduceException<string, string> produceException)
            {
                return false;
            }
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
}