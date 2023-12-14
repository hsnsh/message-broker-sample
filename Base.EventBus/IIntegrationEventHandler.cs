namespace Base.EventBus;

public interface IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
}