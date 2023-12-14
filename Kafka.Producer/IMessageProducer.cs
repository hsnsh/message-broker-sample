using Base.EventBus;

namespace Kafka.Producer;

public interface IMessageProducer
{
    bool Produce(IntegrationEvent message, string topic, string key = null);

    Task<bool> ProduceAsync(IntegrationEvent message, string topic, string key = null);
}