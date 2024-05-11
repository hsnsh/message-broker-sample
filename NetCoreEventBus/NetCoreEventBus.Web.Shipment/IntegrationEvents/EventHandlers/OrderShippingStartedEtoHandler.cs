using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Shipment.Services;

namespace NetCoreEventBus.Web.Shipment.IntegrationEvents.EventHandlers;

public class OrderShippingStartedEtoHandler : IIntegrationEventHandler<OrderShippingStartedEto>
{
    private readonly ILogger<OrderShippingStartedEtoHandler> _logger;
    private readonly IShipmentService _shipmentService;

    public OrderShippingStartedEtoHandler(ILogger<OrderShippingStartedEtoHandler> logger, IShipmentService shipmentService)
    {
        _logger = logger;
        _shipmentService = shipmentService;
    }

    public async Task HandleAsync(OrderShippingStartedEto @event)
    {
        _logger.LogInformation(@event.ToString());
        await _shipmentService.OrderShippingStartedAsync(@event);
    }
}