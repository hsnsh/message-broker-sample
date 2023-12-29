namespace Base.EventBus.Abstractions;

public interface IIntegrationEventHandler<TEventMessage> : IIntegrationEventHandler
    where TEventMessage : IIntegrationEventMessage
{
    Task HandleAsync(MessageEnvelope<TEventMessage> @event);
}


public interface IIntegrationEventHandler
{
}