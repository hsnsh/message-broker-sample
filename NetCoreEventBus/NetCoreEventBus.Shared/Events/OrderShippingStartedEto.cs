using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Shared.Events;

public record OrderShippingStartedEto(Guid OrderId) : Event
{
    public Guid OrderId { get; } = OrderId;
}