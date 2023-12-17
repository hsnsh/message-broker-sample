using Base.EventBus;
using Shared;

namespace OrderAPI.EventHandlers;

public sealed class OrderStartedIntegrationEventHandler : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    private readonly ILogger<OrderStartedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public OrderStartedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<OrderStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        var space = typeof(OrderStartedIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        _eventBus.Publish(new OrderShippingStartedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, @event.OrderId));

        await Task.CompletedTask;
    }
}