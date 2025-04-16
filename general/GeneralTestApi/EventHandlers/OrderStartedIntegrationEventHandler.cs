using GeneralLibrary.Base.Domain.Entities.Events;
using GeneralLibrary.Base.EventBus;
using GeneralLibrary.Events;

namespace GeneralTestApi.EventHandlers;

public sealed class OrderStartedIntegrationEventHandler : IIntegrationEventHandler<OrderStartedEto>
{
    private readonly IEventBus _eventBus;

    public OrderStartedIntegrationEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public async Task HandleAsync(MessageEnvelope<OrderStartedEto> @event)
    {
        var parentIntegrationEvent = new MessageEnvelope
        {
            HopLevel = @event.HopLevel,
            MessageId = @event.MessageId,
            CorrelationId = @event.CorrelationId,
            UserId = @event.UserId,
            UserRoleUniqueName = @event.UserRoleUniqueName,
            Channel = @event.Channel,
            Producer = @event.Producer
        };
        
        await _eventBus.PublishAsync(new OrderStartedEto(Guid.NewGuid()), parentIntegrationEvent);

        await Task.Delay(5000);

        // throw new Exception("VERITABANINA BAGLANAMADIM");
    }
}