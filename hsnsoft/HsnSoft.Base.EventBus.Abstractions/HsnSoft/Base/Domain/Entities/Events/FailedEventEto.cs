using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public record FailedEventEto(
    [NotNull] string FailedReason,
    [CanBeNull] DateTimeOffset? FailedMessageEnvelopeTime,
    [CanBeNull] dynamic FailedMessageObject,
    [CanBeNull] string FailedMessageTypeName
) : IIntegrationEventMessage
{
    [NotNull]
    public string FailedReason { get; } = FailedReason;

    [CanBeNull]
    public DateTimeOffset? FailedMessageEnvelopeTime { get; } = FailedMessageEnvelopeTime;

    [CanBeNull]
    public object FailedMessageObject { get; } = FailedMessageObject;

    [CanBeNull]
    public string FailedMessageTypeName { get; } = FailedMessageTypeName;
}