using Base.EventBus;
using Shared;

namespace ShipmentAPI.EventHandlers;

public sealed class ShipmentStartedIntegrationEventHandler : IIntegrationEventHandler<ShipmentStartedIntegrationEvent>
{
    private readonly ILogger<ShipmentStartedIntegrationEventHandler> _logger;

    public ShipmentStartedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        // _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger = loggerFactory.CreateLogger<ShipmentStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(ShipmentStartedIntegrationEvent @event)
    {
        var space = typeof(ShipmentStartedIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event,space);

        // Simulate a work time
        await Task.Delay(10000);

        await Task.CompletedTask;
    }
}