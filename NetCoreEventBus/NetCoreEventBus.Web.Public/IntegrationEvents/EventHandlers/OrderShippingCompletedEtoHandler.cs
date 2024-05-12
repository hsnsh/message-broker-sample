using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Public.IntegrationEvents.EventHandlers;

public class OrderShippingCompletedEtoHandler : IIntegrationEventHandler<OrderShippingCompletedEto>
{
    private readonly IBaseLogger<OrderShippingCompletedEtoHandler> _logger;

    public OrderShippingCompletedEtoHandler(IBaseLogger<OrderShippingCompletedEtoHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(MessageEnvelope<OrderShippingCompletedEto> @event)
    {
        _logger.LogInformation(@event.ToString());
        return Task.CompletedTask;
    }
}