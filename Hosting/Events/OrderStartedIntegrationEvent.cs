using HsnSoft.Base.Domain.Entities.Events;

namespace Hosting.Events;

public record OrderStartedIntegrationEvent(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}