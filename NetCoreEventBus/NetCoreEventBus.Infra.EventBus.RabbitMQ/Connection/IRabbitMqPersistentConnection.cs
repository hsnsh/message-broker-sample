using JetBrains.Annotations;
using RabbitMQ.Client;

namespace NetCoreEventBus.Infra.EventBus.RabbitMQ.Connection;

public interface IRabbitMqPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    [CanBeNull]
    IModel CreateModel();
}