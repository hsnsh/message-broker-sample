using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Shared.Events;

public record OrderShippingStartedEto(Guid OrderId) : IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}