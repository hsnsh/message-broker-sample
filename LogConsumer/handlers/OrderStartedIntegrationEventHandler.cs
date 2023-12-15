using Base.EventBus;
using Microsoft.Extensions.Logging;
using Shared;

namespace LogConsumer.handlers;

public sealed class OrderStartedIntegrationEventHandler : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    private readonly ILogger<OrderStartedIntegrationEventHandler> _logger;

    public OrderStartedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        // _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger = loggerFactory.CreateLogger<OrderStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        var space = typeof(OrderStartedIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, space, @event);

        // Simulate a work time
        await Task.Delay(1000);

        await Task.CompletedTask;
    }
}