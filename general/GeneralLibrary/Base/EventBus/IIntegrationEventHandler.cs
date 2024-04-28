using GeneralLibrary.Base.Domain.Entities.Events;

namespace GeneralLibrary.Base.EventBus;

public interface IIntegrationEventHandler<TEventMessage> : IIntegrationEventHandler
    where TEventMessage : IIntegrationEventMessage
{
    Task HandleAsync(MessageEnvelope<TEventMessage> @event);
}

public interface IIntegrationEventHandler
{
}