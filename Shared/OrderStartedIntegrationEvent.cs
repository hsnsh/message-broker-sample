using Base.EventBus;

namespace Shared;

public record OrderStartedIntegrationEvent(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}