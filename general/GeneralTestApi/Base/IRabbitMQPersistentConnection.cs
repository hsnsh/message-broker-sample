using JetBrains.Annotations;
using RabbitMQ.Client;

namespace GeneralTestApi.Base;

public interface IRabbitMqPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    [CanBeNull]
    IModel CreateModel();
}