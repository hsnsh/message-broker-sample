using System.Text.Json.Serialization;

namespace Base.EventBus;

public abstract class IntegrationEvent
{
    [JsonInclude]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [JsonInclude]
    public DateTime CreationDate { get; private set; } = DateTime.UtcNow;
}