using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Shared.Events;

public record ShipmentStartedEto(Guid OrderId, Guid ShipmentId)  : IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
    public Guid ShipmentId { get; } = ShipmentId;
}