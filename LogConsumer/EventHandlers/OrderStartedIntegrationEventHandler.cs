using Base.EventBus;
using Microsoft.Extensions.Logging;
using Shared;

namespace LogConsumer.EventHandlers;

public sealed class OrderStartedIntegrationEventHandler : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    private readonly ILogger<OrderStartedIntegrationEventHandler> _logger;

    public OrderStartedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrderStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        var space = typeof(OrderStartedIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(200);

        await Task.CompletedTask;
    }
}