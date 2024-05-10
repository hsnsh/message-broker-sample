using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Shipment.Services;

public interface IShipmentService 
{
    Task OrderShippingStartedAsync(OrderShippingStartedEto input, CancellationToken cancellationToken = default);
    Task ShipmentStartedAsync(ShipmentStartedEto input, CancellationToken cancellationToken = default);
}