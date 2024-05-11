using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Infra.EventBus.Logging;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Shipment.Services;

namespace NetCoreEventBus.Web.Shipment.IntegrationEvents.EventHandlers;

public class OrderShippingStartedEtoHandler : IIntegrationEventHandler<OrderShippingStartedEto>
{
    private readonly IBaseLogger _logger;
    private readonly IShipmentService _shipmentService;

    public OrderShippingStartedEtoHandler(IBaseLogger logger, IShipmentService shipmentService)
    {
        _logger = logger;
        _shipmentService = shipmentService;
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingStartedEto> @event)
    {
        _logger.LogInformation(@event.ToString());
        await _shipmentService.OrderShippingStartedAsync(@event.Message);
    }
}