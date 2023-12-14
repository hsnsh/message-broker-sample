namespace Base.EventBus;

public interface IIntegrationEventHandler<TEvent> : IntegrationEventHandler
    where TEvent : IntegrationEvent
{
    Task Handle(TEvent @event);
}


public interface IntegrationEventHandler
{
}