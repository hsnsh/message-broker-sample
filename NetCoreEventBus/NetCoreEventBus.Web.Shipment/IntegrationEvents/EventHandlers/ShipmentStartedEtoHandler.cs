using NetCoreEventBus.Infra.EventBus.Events;
using NetCoreEventBus.Infra.EventBus.Logging;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Shipment.Services;

namespace NetCoreEventBus.Web.Shipment.IntegrationEvents.EventHandlers;

public class ShipmentStartedEtoHandler : IIntegrationEventHandler<ShipmentStartedEto>
{
    private readonly IBaseLogger _logger;
    private readonly IShipmentService _shipmentService;

    public ShipmentStartedEtoHandler(IBaseLogger logger, IShipmentService shipmentService)
    {
        _logger = logger;
        _shipmentService = shipmentService;
    }

    public async Task HandleAsync(MessageEnvelope<ShipmentStartedEto> @event)
    {
        _logger.LogInformation(@event.ToString());
        await _shipmentService.ShipmentStartedAsync(@event.Message);
    }
}