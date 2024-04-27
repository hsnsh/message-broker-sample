using JetBrains.Annotations;

namespace GeneralLibrary.Base;

public interface IEventBus
{
    Task PublishAsync<TEventMessage>([NotNull] TEventMessage eventMessage, [CanBeNull] ParentMessageEnvelope parentMessage = null, bool isReQueuePublish = false) where TEventMessage : IIntegrationEventMessage;

    void Subscribe<TEvent, THandler>()
        where TEvent : IIntegrationEventMessage
        where THandler : IIntegrationEventHandler<TEvent>;

    // void Subscribe(Type eventType, Type eventHandlerType);
    //
    // void Unsubscribe<TEvent, THandler>()
    //     where TEvent : IIntegrationEventMessage
    //     where THandler : IIntegrationEventHandler<TEvent>;
}