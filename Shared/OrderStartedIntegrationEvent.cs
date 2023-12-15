using Base.EventBus;

namespace Shared;

public sealed class OrderStartedIntegrationEvent : BaseIntegrationEvent
{
    public Guid OrderId { get; }

    // public OrderStartedIntegrationEvent(Guid orderId) : this(Guid.NewGuid(), DateTime.UtcNow, orderId)
    // {
    // }

    [Newtonsoft.Json.JsonConstructor] // Json Deserialize Constructor
    public OrderStartedIntegrationEvent(Guid id, DateTime creationTime, Guid orderId) : base(id, creationTime)
    {
        Id = id;
        CreationTime = creationTime;
        OrderId = orderId;
    }
}