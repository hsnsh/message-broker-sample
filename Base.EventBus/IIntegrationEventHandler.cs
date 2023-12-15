namespace Base.EventBus;

public interface IIntegrationEventHandler<TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    Task Handle(TEvent @event);
}


public interface IIntegrationEventHandler
{
}