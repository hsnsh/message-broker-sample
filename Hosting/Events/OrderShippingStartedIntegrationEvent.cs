using HsnSoft.Base.Domain.Entities.Events;

namespace Hosting.Events;

public record OrderShippingStartedIntegrationEvent(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}