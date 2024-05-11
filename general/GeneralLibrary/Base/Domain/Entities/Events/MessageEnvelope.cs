using JetBrains.Annotations;

namespace GeneralLibrary.Base.Domain.Entities.Events;

public class MessageEnvelope
{
    public int HopLevel { get; set; }

    public bool IsReQueued { get; set; }
    public int ReQueueCount { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }

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

public sealed class MessageEnvelope<T> : MessageEnvelope where T : IIntegrationEventMessage
{
    public Guid? ParentMessageId { get; set; }
    
    public T Message { get; set; }
}