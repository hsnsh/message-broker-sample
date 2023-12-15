using System.Text.Json.Serialization;

namespace Base.EventBus;

public interface IIntegrationEvent
{
    public Guid Id { get; }

    public DateTime CreationTime { get; }
}

public abstract class BaseIntegrationEvent : IIntegrationEvent
{
    [JsonInclude]
    public Guid Id { get; }

    [JsonInclude]
    public DateTime CreationTime { get; }

    [Newtonsoft.Json.JsonConstructor]
    protected BaseIntegrationEvent(Guid id, DateTime creationTime)
    {
        Id = id;
        CreationTime = creationTime;
    }
}