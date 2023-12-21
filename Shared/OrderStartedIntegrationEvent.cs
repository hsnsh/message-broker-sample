using Base.EventBus;

namespace Shared;

public record OrderStartedIntegrationEvent(Guid Id, DateTime CreationTime, Guid OrderId) : IntegrationEvent(Id, CreationTime)
{
    public Guid OrderId { get; } = OrderId;
}