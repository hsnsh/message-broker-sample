using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Order.Services;

namespace NetCoreEventBus.Web.Order.IntegrationEvents.EventHandlers;

public class OrderStartedEtoHandler : IIntegrationEventHandler<OrderStartedEto>
{
    private readonly IBaseLogger _logger;
    private readonly IOrderService _orderService;

    public OrderStartedEtoHandler(IBaseLogger logger, IOrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }

    public async Task HandleAsync(MessageEnvelope<OrderStartedEto> @event)
    {
        _logger.LogInformation("OrderStarted BEGIN => OrderNo{OrderNo}", @event.Message.OrderNo.ToString());
        await _orderService.OrderStartedAsync(@event.Message);
        _logger.LogInformation("OrderStarted END => OrderNo{OrderNo}", @event.Message.OrderNo.ToString());
    }
}