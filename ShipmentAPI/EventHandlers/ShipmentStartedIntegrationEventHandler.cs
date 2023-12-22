using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Abstractions;

namespace ShipmentAPI.EventHandlers;

public sealed class ShipmentStartedIntegrationEventHandler : IIntegrationEventHandler<ShipmentStartedIntegrationEvent>
{
    private readonly ILogger<ShipmentStartedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public ShipmentStartedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<ShipmentStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task HandleAsync(MessageEnvelope<ShipmentStartedIntegrationEvent> @event)
    {
        var space = typeof(ShipmentStartedIntegrationEventHandler).Namespace;
        _logger.LogDebug("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        await _eventBus.PublishAsync(new OrderShippingCompletedIntegrationEvent(@event.Message.OrderId, @event.Message.ShipmentId),
            relatedMessageId: @event.MessageId,
            correlationId: @event.CorrelationId);

        await Task.CompletedTask;
    }
}