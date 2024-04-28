using GeneralLibrary.Base.Domain.Entities.Events;

namespace GeneralLibrary.Events;

public record OrderStartedEto(Guid OrderId) : IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}