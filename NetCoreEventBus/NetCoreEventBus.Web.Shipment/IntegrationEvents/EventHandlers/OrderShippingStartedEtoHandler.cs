using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Shipment.Services;

namespace NetCoreEventBus.Web.Shipment.IntegrationEvents.EventHandlers;

public class OrderShippingStartedEtoHandler : IIntegrationEventHandler<OrderShippingStartedEto>
{
    private readonly IBaseLogger<OrderShippingStartedEtoHandler> _logger;
    private readonly IShipmentService _shipmentService;

    public OrderShippingStartedEtoHandler(IBaseLogger<OrderShippingStartedEtoHandler> logger, IShipmentService shipmentService)
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