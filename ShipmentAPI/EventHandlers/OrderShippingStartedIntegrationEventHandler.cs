using Base.EventBus.Abstractions;
using Hosting.Events;

namespace ShipmentAPI.EventHandlers;

public sealed class OrderShippingStartedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingStartedIntegrationEvent>
{
    private readonly ILogger<OrderShippingStartedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public OrderShippingStartedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<OrderShippingStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingStartedIntegrationEvent> @event)
    {
        var space = typeof(OrderShippingStartedIntegrationEvent).Namespace;
        _logger.LogDebug("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        await _eventBus.PublishAsync(new ShipmentStartedIntegrationEvent(@event.Message.OrderId, Guid.NewGuid()),
            parentMessageId: @event.MessageId,
            correlationId: @event.CorrelationId);

        await Task.CompletedTask;
    }
}