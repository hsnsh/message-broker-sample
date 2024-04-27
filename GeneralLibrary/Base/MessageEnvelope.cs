using JetBrains.Annotations;

namespace GeneralLibrary.Base;

public sealed record MessageEnvelope<T> where T : IIntegrationEventMessage
{
    public int HopLevel2 { get; set; }
    
    public bool IsReQueued { get; set; }
    public int ReQueueCount { get; set; }

    public Guid? ParentMessageId { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }

    public T Message { get; set; }

    [CanBeNull]
    public string CorrelationId { get; set; }

    [CanBeNull]
    public string UserId { get; set; }
    [CanBeNull]
    public string UserRoleUniqueName { get; set; }

    [CanBeNull]
    public string Channel { get; set; }
    [CanBeNull]
    public string Producer { get; set; }
}

// Marker
public interface IIntegrationEventMessage
{
}