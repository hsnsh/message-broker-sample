using Base.EventBus;
using Shared;

namespace OrderAPI.EventHandlers;

public sealed class OrderStatusShippedIntegrationEventHandler : IIntegrationEventHandler<OrderStatusShippedIntegrationEvent>
{
    private readonly ILogger<OrderStatusShippedIntegrationEventHandler> _logger;

    public OrderStatusShippedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        // _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger = loggerFactory.CreateLogger<OrderStatusShippedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderStatusShippedIntegrationEvent @event)
    {
        var space = typeof(OrderStatusShippedIntegrationEvent).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event,space);

        // Simulate a work time
        await Task.Delay(1000);

        await Task.CompletedTask;
    }
}