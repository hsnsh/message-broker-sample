using JetBrains.Annotations;
using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Infra.EventBus.Subscriptions;

public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, List<Subscription>> _handlers = new();
    private readonly Dictionary<string, Type> _eventTypes = new();

    [CanBeNull]
    public Func<string, string> EventNameGetter { get; set; }

    public bool IsEmpty => !_handlers.Keys.Any();

    public void AddSubscription<TEvent, TEventHandler>()
        where TEvent : Event
        where TEventHandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();

        var eventHandlerName = typeof(TEventHandler).Name;
        DoAddSubscription(typeof(TEvent), typeof(TEventHandler), eventName);

        _eventTypes.TryAdd(eventName, typeof(TEvent));
    }

    public void Clear()
    {
        _handlers.Clear();
        _eventTypes.Clear();
    }

    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    public string GetEventKey<T>()
    {
        return GetEventKey(typeof(T));
    }

    public string GetEventKey(Type eventType)
    {
        return EventNameGetter != null ? EventNameGetter.Invoke(eventType.Name) : eventType.Name;
    }

    public Type GetEventTypeByName(string eventName) => _eventTypes[eventName];

    public IEnumerable<Subscription> GetHandlersForEvent(string eventName) => _handlers[eventName];

    private void DoAddSubscription(Type eventType, Type handlerType, string eventName)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<Subscription>());
        }

        if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
        }

        _handlers[eventName].Add(new Subscription(eventType, handlerType));
    }
}