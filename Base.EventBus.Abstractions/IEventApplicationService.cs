using JetBrains.Annotations;

namespace Base.EventBus.Abstractions;

public interface IEventApplicationService
{
    [CanBeNull]
    public ParentMessageEnvelope ParentIntegrationEvent { get; set; }
}