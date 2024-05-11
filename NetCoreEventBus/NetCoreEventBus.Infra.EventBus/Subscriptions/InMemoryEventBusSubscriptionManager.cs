using JetBrains.Annotations;
using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Infra.EventBus.Subscriptions;

public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    #region Fields

    private readonly Dictionary<string, List<Subscription>> _handlers = new Dictionary<string, List<Subscription>>();
    private readonly Dictionary<string, Type> _eventTypes = new Dictionary<string, Type>();

    #endregion

    #region Event Handlers

    [CanBeNull]
    public Func<string, string> EventNameGetter { get; set; }

    public event EventHandler<string> OnEventRemoved;

    #endregion

    #region Events info

    public Type GetEventTypeByName(string eventName) => _eventTypes[eventName];

    public string GetEventKey(Type eventType)
    {
        return EventNameGetter != null ? EventNameGetter.Invoke(eventType.Name) : eventType.Name;
    }

    public string GetEventKey<T>()
    {
        return GetEventKey(typeof(T));
    }

    public IEnumerable<Subscription> GetHandlersForEvent(string eventName) => _handlers[eventName];

    /// <summary>
    /// Returns the dictionary of subscriptiosn in an immutable way.
    /// </summary>
    /// <returns>Dictionary.</returns>
    public Dictionary<string, List<Subscription>> GetAllSubscriptions() => new Dictionary<string, List<Subscription>>(_handlers);

    #endregion

    #region Subscriptions management

    public void AddSubscription<TEvent, TEventHandler>()
        where TEvent : Event
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();

        var eventHandlerName = typeof(TEventHandler).Name;
        DoAddSubscription(typeof(TEvent), typeof(TEventHandler), eventName);

        _eventTypes.TryAdd(eventName, typeof(TEvent));
    }

    public void RemoveSubscription<TEvent, TEventHandler>()
        where TEventHandler : IEventHandler<TEvent>
        where TEvent : Event
    {
        var handlerToRemove = FindSubscriptionToRemove<TEvent, TEventHandler>();
        var eventName = GetEventKey<TEvent>();
        DoRemoveHandler(eventName, handlerToRemove);
    }

    public void Clear()
    {
        _handlers.Clear();
        _eventTypes.Clear();
    }

    #endregion

    #region Status

    public bool IsEmpty => !_handlers.Keys.Any();

    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    #endregion

    #region Private methods

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

    private void DoRemoveHandler(string eventName, Subscription subscriptionToRemove)
    {
        if (subscriptionToRemove == null)
        {
            return;
        }

        _handlers[eventName].Remove(subscriptionToRemove);
        if (_handlers[eventName].Any())
        {
            return;
        }

        _handlers.Remove(eventName);
        _eventTypes.Remove(eventName);

        RaiseOnEventRemoved(eventName);
    }

    private void RaiseOnEventRemoved(string eventName)
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }

    private Subscription FindSubscriptionToRemove<TEvent, TEventHandler>()
        where TEvent : Event
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();
        return DoFindSubscriptionToRemove(eventName, typeof(TEventHandler));
    }

    private Subscription DoFindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            return null;
        }

        return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
    }

    #endregion
}