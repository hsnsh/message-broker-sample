using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Shipment.Services;

namespace NetCoreEventBus.Web.Shipment.IntegrationEvents.EventHandlers;

public class ShipmentStartedEtoHandler : IEventHandler<ShipmentStartedEto>
{
    private readonly ILogger<ShipmentStartedEtoHandler> _logger;
    private readonly IShipmentService _shipmentService;

    public ShipmentStartedEtoHandler(ILogger<ShipmentStartedEtoHandler> logger, IShipmentService shipmentService)
    {
        _logger = logger;
        _shipmentService = shipmentService;
    }

    public async Task HandleAsync(ShipmentStartedEto @event)
    {
        _logger.LogInformation(@event.ToString());
        await _shipmentService.ShipmentStartedAsync(@event);
    }
}