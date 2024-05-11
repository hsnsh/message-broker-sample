using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Order.Services;

public sealed class OrderService : IOrderService
{
    private readonly IEventBus _eventBus;
    private readonly IBaseLogger _logger;

    public OrderService(IEventBus eventBus, IBaseLogger logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task OrderStartedAsync(OrderStartedEto input, CancellationToken cancellationToken = default)
    {
        await Task.Delay(5000, cancellationToken);

        // var random = new Random().Next(1, 5) * 1000;
        // _logger.LogInformation("PROCESSING ESTIMATED TIME [{OrderNo}] {Time}", input.OrderNo, random * 5);
        // Thread.Sleep(random * 5);
        // await Task.Delay(random * 5, cancellationToken);

       await _eventBus.PublishAsync(new OrderShippingStartedEto(input.OrderId));
    }

    public async Task OrderShippingCompletedAsync(OrderShippingCompletedEto input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(5000, cancellationToken);

        await Task.CompletedTask;
    }
}