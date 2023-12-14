using System.Threading.Tasks;
using Kafka.Message;
using Shared;

namespace Kafka.Producer
{
    public interface IMessageProducer
    {
        bool Produce(IIntegrationEvent message, string topic, string key = null);

        Task<bool> ProduceAsync(IIntegrationEvent message, string topic, string key = null);
    }
}