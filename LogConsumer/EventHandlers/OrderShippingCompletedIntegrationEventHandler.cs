using Base.EventBus;
using Microsoft.Extensions.Logging;
using Shared;

namespace LogConsumer.EventHandlers;

public sealed class OrderShippingCompletedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingCompletedIntegrationEvent>
{
    private readonly ILogger<OrderShippingCompletedIntegrationEventHandler> _logger;

    public OrderShippingCompletedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrderShippingCompletedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderShippingCompletedIntegrationEvent @event)
    {
        var space = typeof(OrderShippingCompletedIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(200);

        await Task.CompletedTask;
    }
}