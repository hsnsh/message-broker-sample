using Base.EventBus;

namespace Hosting.Events;

public record OrderStartedIntegrationEvent(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}