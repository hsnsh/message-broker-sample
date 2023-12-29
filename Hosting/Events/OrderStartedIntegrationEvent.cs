using Base.EventBus.Abstractions;

namespace Hosting.Events;

public record OrderStartedIntegrationEvent(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}