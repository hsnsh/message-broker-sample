using System.Threading.Tasks;
using Base.EventBus;
using Kafka.Message;

namespace Kafka.Producer
{
    public interface IMessageProducer
    {
        bool Produce(IntegrationEvent message, string topic, string key = null);

        Task<bool> ProduceAsync(IntegrationEvent message, string topic, string key = null);
    }
}