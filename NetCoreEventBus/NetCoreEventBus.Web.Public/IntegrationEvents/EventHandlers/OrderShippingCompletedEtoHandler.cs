using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Infra.EventBus.Logging;
using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Public.IntegrationEvents.EventHandlers;

public class OrderShippingCompletedEtoHandler : IIntegrationEventHandler<OrderShippingCompletedEto>
{
    private readonly IBaseLogger _logger;

    public OrderShippingCompletedEtoHandler(IBaseLogger logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(MessageEnvelope<OrderShippingCompletedEto> @event)
    {
        _logger.LogInformation(@event.ToString());
        return Task.CompletedTask;
    }
}