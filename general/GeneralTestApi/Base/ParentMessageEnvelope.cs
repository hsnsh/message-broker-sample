using JetBrains.Annotations;

namespace GeneralTestApi.Base;

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

public sealed record ErrorMessageEnvelope
{
    public int HopLevel { get; set; }

    public Guid? ParentMessageId { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }

    public object Message { get; set; }

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