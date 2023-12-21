using Base.EventBus;

namespace Shared;

public record ShipmentStartedIntegrationEvent(Guid OrderId, Guid ShipmentId) : IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
    public Guid ShipmentId { get; } = ShipmentId;
}