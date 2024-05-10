using NetCoreEventBus.Infra.EventBus.Bus;
using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Order.Services;

public sealed class OrderService : IOrderService
{
    private readonly IEventBus _eventBus;

    public OrderService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task OrderStartedAsync(OrderStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(5000, cancellationToken);

        _eventBus.Publish(new OrderShippingStartedEto(input.OrderId));
    }

    public async Task OrderShippingCompletedAsync(OrderShippingCompletedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(5000, cancellationToken);

        await Task.CompletedTask;
    }
}