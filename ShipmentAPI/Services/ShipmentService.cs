using Hosting;
using Hosting.Events;

namespace ShipmentAPI.Services;

public sealed class ShipmentService : BaseServiceAppService, IShipmentService
{
    public ShipmentService(IServiceProvider provider) : base(provider)
    {
    }

    public async Task OrderShippingStartedAsync(OrderShippingStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        // await Task.Delay(100, cancellationToken);

        await EventBus.PublishAsync(new ShipmentStartedEto(input.OrderId,Guid.NewGuid()), ParentIntegrationEvent);
    }

    public async Task ShipmentStartedAsync(ShipmentStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        // await Task.Delay(100, cancellationToken);
      
        await EventBus.PublishAsync(new OrderShippingCompletedEto(input.OrderId, input.ShipmentId), ParentIntegrationEvent);

        await Task.CompletedTask;
    }
}