using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared.Events;

namespace NetCoreEventBus.Web.Public.IntegrationEvents.EventHandlers;

public class OrderShippingCompletedEtoHandler : IIntegrationEventHandler<OrderShippingCompletedEto>
{
    private readonly IBaseLogger _logger;

    public OrderShippingCompletedEtoHandler(IBaseLogger logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingCompletedEto> @event)
    {
        _logger.LogInformation(@event.ToString());

        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(1000);
    }
}