using HsnSoft.Base.Domain.Entities.Events;

namespace NetCoreEventBus.Shared.Events;

public record OrderShippingCompletedEto(Guid OrderId, Guid ShipmentId) : IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
    public Guid ShipmentId { get; } = ShipmentId;
}