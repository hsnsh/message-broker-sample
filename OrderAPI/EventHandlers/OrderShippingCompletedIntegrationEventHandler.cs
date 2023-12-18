using Base.EventBus;
using Shared;

namespace OrderAPI.EventHandlers;

public sealed class OrderShippingCompletedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingCompletedIntegrationEvent>
{
    private readonly ILogger<OrderShippingCompletedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public OrderShippingCompletedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<OrderShippingCompletedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderShippingCompletedIntegrationEvent @event)
    {
        var space = typeof(OrderShippingCompletedIntegrationEventHandler).Namespace;
        _logger.LogDebug("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        await Task.CompletedTask;
    }
}