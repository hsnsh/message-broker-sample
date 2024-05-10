using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Order.Services;

public interface IOrderService
{
    Task OrderStartedAsync(OrderStartedEto input, CancellationToken cancellationToken = default);
    Task OrderShippingCompletedAsync(OrderShippingCompletedEto input, CancellationToken cancellationToken = default);
}