namespace Base.EventBus;

public interface IEventBus
{
    Task Publish(IntegrationEvent @event);

    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;

    void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;
}

//
// public interface IEventBus
// {
//     Task PublishAsync<TEvent>(TEvent eventData, bool onUnitOfWorkComplete = true) 
//         where TEvent : class;
//
//     Task PublishAsync(Type eventType, object eventData, bool onUnitOfWorkComplete = true);
//
//     IDisposable Subscribe<TEvent>(Func<TEvent, Task> action) 
//         where TEvent : class;
//
//     IDisposable Subscribe<TEvent, THandler>()
//         where TEvent : class
//         where THandler : IEventHandler, new();
//
//     IDisposable Subscribe(Type eventType, IEventHandler handler);
//
//     IDisposable Subscribe<TEvent>(IEventHandlerFactory factory)
//         where TEvent : class;
//
//     IDisposable Subscribe(Type eventType, IEventHandlerFactory factory);
//
//     void Unsubscribe<TEvent>(Func<TEvent, Task> action)
//         where TEvent : class;
//
//     void Unsubscribe(Type eventType, IEventHandler handler);
//
//     void Unsubscribe<TEvent>(IEventHandlerFactory factory)
//         where TEvent : class;
//
//     void Unsubscribe(Type eventType, IEventHandlerFactory factory);
//
//     void UnsubscribeAll<TEvent>()
//         where TEvent : class;
//
//     void UnsubscribeAll(Type eventType);
// }