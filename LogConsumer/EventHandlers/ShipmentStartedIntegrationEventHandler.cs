using Base.EventBus;
using Microsoft.Extensions.Logging;
using Shared;

namespace LogConsumer.EventHandlers;

public sealed class ShipmentStartedIntegrationEventHandler : IIntegrationEventHandler<ShipmentStartedIntegrationEvent>
{
    private readonly ILogger<ShipmentStartedIntegrationEventHandler> _logger;

    public ShipmentStartedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ShipmentStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(ShipmentStartedIntegrationEvent @event)
    {
        var space = typeof(ShipmentStartedIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event,space);

        // Simulate a work time
        await Task.Delay(200);
        
        await Task.CompletedTask;
    }
}