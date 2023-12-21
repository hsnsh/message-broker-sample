using Base.EventBus;

namespace Shared;

public record OrderShippingCompletedIntegrationEvent(Guid Id, DateTime CreationTime, Guid OrderId, Guid ShipmentId) : IntegrationEvent(Id, CreationTime)
{
    public Guid OrderId { get; } = OrderId;
    public Guid ShipmentId { get; } = ShipmentId;
}