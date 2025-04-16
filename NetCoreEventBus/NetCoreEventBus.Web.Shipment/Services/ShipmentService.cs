using HsnSoft.Base.EventBus;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Shipment.Infra.Domain;

namespace NetCoreEventBus.Web.Shipment.Services;

public sealed class ShipmentService : IShipmentService
{
    private readonly IEventBus _eventBus;
    private readonly IContentGenericRepository<ShipmentEntity> _genericRepository;

    public ShipmentService(IEventBus eventBus, IContentGenericRepository<ShipmentEntity> genericRepository)
    {
        _eventBus = eventBus;
        _genericRepository = genericRepository;
    }

    public async Task OrderShippingStartedAsync(OrderShippingStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(1000, cancellationToken);

        var shipmentId = Guid.NewGuid();
        // await _genericRepository.InsertAsync(new ShipmentEntity(shipmentId, input.OrderId.ToString()), cancellationToken);

        await _eventBus.PublishAsync(new ShipmentStartedEto(input.OrderId, shipmentId));
    }

    public async Task ShipmentStartedAsync(ShipmentStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(1000, cancellationToken);

        await _eventBus.PublishAsync(new OrderShippingCompletedEto(input.OrderId, input.ShipmentId));

        await Task.CompletedTask;
    }
}