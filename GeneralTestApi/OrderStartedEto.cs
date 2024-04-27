using GeneralTestApi.Base;

namespace GeneralTestApi;

public record OrderStartedEto(Guid OrderId): IIntegrationEventMessage
{
    public Guid OrderId { get; } = OrderId;
}