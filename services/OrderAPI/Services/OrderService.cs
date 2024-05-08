using Hosting;
using Hosting.Events;

namespace OrderAPI.Services;

public sealed class OrderService : BaseServiceAppService, IOrderService
{
    public OrderService(IServiceProvider provider) : base(provider)
    {
    }

    public async Task OrderStartedAsync(OrderStartedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(1000, cancellationToken);

        await EventBus.PublishAsync(new OrderShippingStartedEto(input.OrderId), ParentIntegrationEvent);
    }

    public async Task OrderShippingCompletedAsync(OrderShippingCompletedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(1000, cancellationToken);

        await Task.CompletedTask;
    }
}