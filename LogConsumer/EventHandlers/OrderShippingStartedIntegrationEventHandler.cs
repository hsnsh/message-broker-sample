using Base.EventBus;
using Microsoft.Extensions.Logging;
using Shared;

namespace LogConsumer.EventHandlers;

public sealed class OrderShippingStartedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingStartedIntegrationEvent>
{
    private readonly ILogger<OrderShippingStartedIntegrationEventHandler> _logger;

    public OrderShippingStartedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrderShippingStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }
    
    public async Task HandleAsync(MessageEnvelope<OrderShippingStartedIntegrationEvent> @event)
    {
        var space = typeof(OrderShippingStartedIntegrationEvent).Namespace;
        _logger.LogDebug("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        await Task.CompletedTask;
    }
}