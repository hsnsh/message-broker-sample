using Base.EventBus;
using Shared;

namespace OrderAPI.EventHandlers;

public sealed class OrderStatusUpdatedIntegrationEventHandler : IIntegrationEventHandler<OrderStatusUpdatedIntegrationEvent>
{
    private readonly ILogger<OrderStatusUpdatedIntegrationEventHandler> _logger;

    public OrderStatusUpdatedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        // _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger = loggerFactory.CreateLogger<OrderStatusUpdatedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderStatusUpdatedIntegrationEvent @event)
    {
        var space = typeof(OrderStatusUpdatedIntegrationEvent).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event,space);

        // Simulate a work time
        await Task.Delay(1000);

        await Task.CompletedTask;
    }
}