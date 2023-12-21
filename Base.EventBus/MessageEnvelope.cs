using JetBrains.Annotations;

namespace Base.EventBus;

public sealed record MessageEnvelope<T> where T : IIntegrationEventMessage
{
    public Guid? RelatedMessageId { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }
    
    public T Message { get; set; }

    [CanBeNull]
    public string Producer { get; set; }

    [CanBeNull]
    public string CorrelationId { get; set; }
}

// Marker
public interface IIntegrationEventMessage
{
}