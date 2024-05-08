using Hosting.Events;
using HsnSoft.Base.EventBus;

namespace OrderAPI.Services;

public interface IOrderService : IEventApplicationService
{
    Task OrderStartedAsync(OrderStartedEto input, CancellationToken cancellationToken = default);
    Task OrderShippingCompletedAsync(OrderShippingCompletedEto input, CancellationToken cancellationToken = default);
}