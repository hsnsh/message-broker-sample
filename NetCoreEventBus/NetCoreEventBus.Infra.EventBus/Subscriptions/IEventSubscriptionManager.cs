using NetCoreEventBus.Infra.EventBus.Events;

namespace NetCoreEventBus.Infra.EventBus.Subscriptions;

public interface IEventBusSubscriptionManager
{
    Func<string, string> EventNameGetter { get; set; }
    bool IsEmpty { get; }

    void AddSubscription<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>;

    void AddSubscription(Type eventType, Type eventHandlerType);

    void Clear();

    bool HasSubscriptionsForEvent(string eventName);
    string GetEventKey<TEvent>();
    Type GetEventTypeByName(string eventName);
    IEnumerable<Subscription> GetHandlersForEvent(string eventName);
}