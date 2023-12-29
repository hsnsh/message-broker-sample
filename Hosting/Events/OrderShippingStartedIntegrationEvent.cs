using Base.EventBus.Abstractions;

namespace Hosting.Events;

public record OrderShippingStartedIntegrationEvent(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}