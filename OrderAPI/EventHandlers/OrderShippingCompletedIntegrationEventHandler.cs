using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Logging;

namespace OrderAPI.EventHandlers;

public sealed class OrderShippingCompletedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingCompletedEto>
{
    private readonly IEventBusLogger _logger;
    private readonly IEventBus _eventBus;

    public OrderShippingCompletedIntegrationEventHandler(IEventBusLogger logger, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingCompletedEto> @event)
    {
        var space = typeof(OrderShippingCompletedIntegrationEventHandler).Namespace;
        _logger.LogDebug("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        await Task.CompletedTask;
    }
}