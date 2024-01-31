using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Logging;

namespace ShipmentAPI.EventHandlers;

public sealed class OrderShippingStartedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingStartedEto>
{
    private readonly IEventBusLogger _logger;
    private readonly IEventBus _eventBus;

    public OrderShippingStartedIntegrationEventHandler(IEventBusLogger logger, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingStartedEto> @event)
    {
        var space = typeof(OrderShippingStartedEto).Namespace;
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

        await _eventBus.PublishAsync(new ShipmentStartedEto(@event.Message.OrderId, Guid.NewGuid()), parentIntegrationEvent);

        await Task.CompletedTask;
    }
}