using Base.EventBus;

namespace Shared;

public sealed class OrderStartedIntegrationEvent : BaseIntegrationEvent
{
    public Guid OrderId { get;  set; }
    
    public OrderStartedIntegrationEvent(Guid orderId) : this(Guid.NewGuid(), DateTime.UtcNow, orderId)
    {
    }
    
    [Newtonsoft.Json.JsonConstructor] // Json Deserialize Constructor
    private OrderStartedIntegrationEvent(Guid id, DateTime creationTime, Guid orderId) : base(id, creationTime)
    {
        OrderId = orderId;
    }
}