using HsnSoft.Base.Domain.Entities.Events;

namespace Shared;

public record OrderStartedIntegrationEvent(Guid Id, DateTime CreationTime, Guid OrderId) : IntegrationEvent(Id, CreationTime)
{
    public Guid OrderId { get; } = OrderId;
}