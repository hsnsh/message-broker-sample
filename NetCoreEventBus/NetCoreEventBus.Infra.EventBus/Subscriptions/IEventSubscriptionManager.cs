using JetBrains.Annotations;
using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Infra.EventBus.Subscriptions;

public interface IEventBusSubscriptionManager
{
    Func<string, string> EventNameGetter { get; set; }
    
    bool IsEmpty { get; }
    void Clear();

    void AddSubscription<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>;

    void AddSubscription(Type eventType, Type eventHandlerType);

    bool HasSubscriptionsForEvent<T>() where T : IIntegrationEventMessage;
    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IIntegrationEventMessage;
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    [CanBeNull]
    Type GetEventTypeByName(string eventName);

    string GetEventKey<T>() where T : IIntegrationEventMessage;

    string GetEventKey(Type eventType);


}