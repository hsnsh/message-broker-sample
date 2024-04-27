using GeneralLibrary.Base;

namespace GeneralLibrary.Events;

public record OrderStartedEto(Guid OrderId) : IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}