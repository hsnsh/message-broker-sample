using Hosting.Events;
using HsnSoft.Base.EventBus;

namespace ShipmentAPI.Services;

public interface IShipmentService : IEventApplicationService
{
    Task OrderShippingStartedAsync(OrderShippingStartedEto input, CancellationToken cancellationToken = default);
    Task ShipmentStartedAsync(ShipmentStartedEto input, CancellationToken cancellationToken = default);
}