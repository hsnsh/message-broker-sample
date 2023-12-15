namespace Base.EventBus;

public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }
    void Clear();
    event EventHandler<string> OnEventRemoved;

    void AddSubscription<T, TH>() where T : IIntegrationEvent where TH : IIntegrationEventHandler<T>;

    void RemoveSubscription<T, TH>() where TH : IIntegrationEventHandler<T> where T : IIntegrationEvent;

    bool HasSubscriptionsForEvent<T>() where T : IIntegrationEvent;
    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IIntegrationEvent;
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    Type GetEventTypeByName(string eventName);

    string GetEventKey<T>();
}