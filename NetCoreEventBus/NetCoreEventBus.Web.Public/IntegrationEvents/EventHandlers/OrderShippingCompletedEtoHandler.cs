using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Public.IntegrationEvents.EventHandlers;

public class OrderShippingCompletedEtoHandler : IIntegrationEventHandler<OrderShippingCompletedEto>
{
    private readonly ILogger<OrderShippingCompletedEtoHandler> _logger;

    public OrderShippingCompletedEtoHandler(ILogger<OrderShippingCompletedEtoHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderShippingCompletedEto @event)
    {
        _logger.LogInformation(@event.ToString());
        return Task.CompletedTask;
    }
}