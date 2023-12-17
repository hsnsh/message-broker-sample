using Base.EventBus;
using Shared;

namespace ShipmentAPI.EventHandlers;

public sealed class OrderShippingStartedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingStartedIntegrationEvent>
{
    private readonly ILogger<OrderShippingStartedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public OrderShippingStartedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<OrderShippingStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderShippingStartedIntegrationEvent @event)
    {
        var space = typeof(OrderShippingStartedIntegrationEvent).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(1000);

        _eventBus.Publish(new ShipmentStartedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, @event.OrderId, Guid.NewGuid()));

        await Task.CompletedTask;
    }
}