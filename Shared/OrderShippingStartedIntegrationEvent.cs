using Base.EventBus;

namespace Shared;

public record OrderShippingStartedIntegrationEvent(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}