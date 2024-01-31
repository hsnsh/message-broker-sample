using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace ShipmentAPI.EventHandlers;

public sealed class ShipmentStartedIntegrationEventHandler : IIntegrationEventHandler<ShipmentStartedEto>
{
    private readonly ILogger<ShipmentStartedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public ShipmentStartedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<ShipmentStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task HandleAsync(MessageEnvelope<ShipmentStartedEto> @event)
    {
        var space = typeof(ShipmentStartedIntegrationEventHandler).Namespace;
        _logger.LogDebug("Handling Integration Event: {@IntegrationEvent} at {AppName}", @event, space);

        // Simulate a work time
        await Task.Delay(5000);

        var parentIntegrationEvent = new ParentMessageEnvelope
        {
            HopLevel = @event.HopLevel,
            MessageId = @event.MessageId,
            CorrelationId = @event.CorrelationId,
            UserId = @event.UserId,
            UserRoleUniqueName = @event.UserRoleUniqueName,
            Channel = @event.Channel,
            Producer = @event.Producer
        };
        
        await _eventBus.PublishAsync(new OrderShippingCompletedEto(@event.Message.OrderId, @event.Message.ShipmentId), parentIntegrationEvent);

        await Task.CompletedTask;
    }
}