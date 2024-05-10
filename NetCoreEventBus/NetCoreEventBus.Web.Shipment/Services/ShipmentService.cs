using NetCoreEventBus.Infra.EventBus.Bus;
using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Shipment.Services;

public sealed class ShipmentService : IShipmentService
{
    private readonly IEventBus _eventBus;

    public ShipmentService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task OrderShippingStartedAsync(OrderShippingStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(5000, cancellationToken);

        _eventBus.Publish(new ShipmentStartedEto(input.OrderId, Guid.NewGuid()));
    }

    public async Task ShipmentStartedAsync(ShipmentStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(5000, cancellationToken);

        _eventBus.Publish(new OrderShippingCompletedEto(input.OrderId, input.ShipmentId));

        await Task.CompletedTask;
    }
}