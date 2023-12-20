using HsnSoft.Base.EventBus.Abstractions;
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

    public async Task HandleAsync(OrderShippingCompletedIntegrationEvent @event)
    {
        var space = typeof(OrderShippingCompletedIntegrationEventHandler).Namespace;
        _logger.LogDebug("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        await Task.CompletedTask;
    }
}