namespace Base.EventBus;

public interface IIntegrationEventHandler<TEvent> : IIntegrationEventHandler
    where TEvent : IIntegrationEvent
{
    Task Handle(TEvent @event);
}


public interface IIntegrationEventHandler
{
}