using JetBrains.Annotations;

namespace Base.EventBus.Abstractions;

public sealed class ParentMessageEnvelope
{
    public int HopLevel { get; set; }

    public Guid MessageId { get; set; }

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