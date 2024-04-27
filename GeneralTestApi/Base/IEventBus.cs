using JetBrains.Annotations;

namespace GeneralTestApi.Base;

public interface IEventBus
{
    Task PublishAsync<TEventMessage>(TEventMessage eventMessage, [CanBeNull] ParentMessageEnvelope parentMessage = null) where TEventMessage : IIntegrationEventMessage;

    void Subscribe<TEvent, THandler>()
        where TEvent : IIntegrationEventMessage
        where THandler : IIntegrationEventHandler<TEvent>;
    
    // void Subscribe(Type eventType, Type eventHandlerType);
    //
    // void Unsubscribe<TEvent, THandler>()
    //     where TEvent : IIntegrationEventMessage
    //     where THandler : IIntegrationEventHandler<TEvent>;
}