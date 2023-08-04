using System.Threading.Tasks;
using Kafka.Message;

namespace Kafka.Producer
{
    public interface IMessageProducer
    {
        bool Produce(IMessageBase message, string topic, string key = null);

        Task<bool> ProduceAsync(IMessageBase message, string topic, string key = null);
    }
}