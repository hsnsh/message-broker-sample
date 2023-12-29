namespace Base.EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync<TEventMessage>(TEventMessage eventMessage, Guid? parentMessageId = null, string? correlationId = null) where TEventMessage : IIntegrationEventMessage;

    void Subscribe<TEvent, THandler>()
        where TEvent : IIntegrationEventMessage
        where THandler : IIntegrationEventHandler<TEvent>;

    void Subscribe(Type eventType, Type eventHandlerType);

    void Unsubscribe<TEvent, THandler>()
        where TEvent : IIntegrationEventMessage
        where THandler : IIntegrationEventHandler<TEvent>;
}