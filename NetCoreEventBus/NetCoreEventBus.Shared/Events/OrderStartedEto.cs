using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Shared.Events;

public record OrderStartedEto(Guid OrderId,int OrderNo) : IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
    public int OrderNo { get; } = OrderNo;
}