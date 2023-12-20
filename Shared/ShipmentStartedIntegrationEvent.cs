using HsnSoft.Base.Domain.Entities.Events;

namespace Shared;

public record ShipmentStartedIntegrationEvent(Guid Id, DateTime CreationTime, Guid OrderId, Guid ShipmentId) : IntegrationEvent(Id, CreationTime)
{
    public Guid OrderId { get; } = OrderId;
    public Guid ShipmentId { get; } = ShipmentId;
}