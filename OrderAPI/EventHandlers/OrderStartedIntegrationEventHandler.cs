using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace OrderAPI.EventHandlers;

public sealed class OrderStartedIntegrationEventHandler : IIntegrationEventHandler<OrderStartedEto>
{
    private readonly ILogger<OrderStartedIntegrationEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public OrderStartedIntegrationEventHandler(ILoggerFactory loggerFactory, IEventBus eventBus)
    {
        _eventBus = eventBus;
        _logger = loggerFactory.CreateLogger<OrderStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task HandleAsync(MessageEnvelope<OrderStartedEto> @event)
    {
        var space = typeof(OrderStartedIntegrationEventHandler).Namespace;
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
        
        await _eventBus.PublishAsync(new OrderShippingStartedEto(@event.Message.OrderId), parentIntegrationEvent);

        await Task.CompletedTask;
    }
}