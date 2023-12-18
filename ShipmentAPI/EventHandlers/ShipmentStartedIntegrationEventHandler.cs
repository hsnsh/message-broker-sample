using Base.EventBus;
using Shared;

namespace ShipmentAPI.EventHandlers;

public sealed class ShipmentStartedIntegrationEventHandler : IIntegrationEventHandler<ShipmentStartedIntegrationEvent>
{
    private readonly ILogger<ShipmentStartedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public ShipmentStartedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<ShipmentStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(ShipmentStartedIntegrationEvent @event)
    {
        var space = typeof(ShipmentStartedIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        await _eventBus.PublishAsync(new OrderShippingCompletedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, @event.OrderId, @event.ShipmentId));

        await Task.CompletedTask;
    }
}