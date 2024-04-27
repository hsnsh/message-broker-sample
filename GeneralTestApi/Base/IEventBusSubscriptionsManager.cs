using JetBrains.Annotations;

namespace GeneralTestApi.Base;

public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }
    void Clear();
    
    void AddSubscription(Type eventType, Type eventHandlerType);
    
    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    [CanBeNull]
    Type GetEventTypeByName(string eventName);
}