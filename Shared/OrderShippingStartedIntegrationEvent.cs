using Base.EventBus;

namespace Shared;

public record OrderShippingStartedIntegrationEvent(Guid Id, DateTime CreationTime, Guid OrderId) : IntegrationEvent(Id, CreationTime)
{
    public Guid OrderId { get; } = OrderId;
}