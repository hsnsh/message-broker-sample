using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Order.Services;

namespace NetCoreEventBus.Web.Order.IntegrationEvents.EventHandlers;

public class OrderShippingCompletedEtoHandler : IEventHandler<OrderShippingCompletedEto>
{
    private readonly ILogger<OrderShippingCompletedEtoHandler> _logger;
    private readonly IOrderService _orderService;

    public OrderShippingCompletedEtoHandler(ILogger<OrderShippingCompletedEtoHandler> logger, IOrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }

    public async Task HandleAsync(OrderShippingCompletedEto @event)
    {
        _logger.LogInformation(@event.ToString());
        await _orderService.OrderShippingCompletedAsync(@event);
    }
}