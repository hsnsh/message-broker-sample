using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Shared.Events;

public record OrderShippingCompletedEto(Guid OrderId, Guid ShipmentId) : Event
{
    public Guid OrderId { get; } = OrderId;
    public Guid ShipmentId { get; } = ShipmentId;
}