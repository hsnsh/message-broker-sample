namespace Base.EventBus;

public interface IIntegrationEventHandler<TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event);
}


public interface IIntegrationEventHandler
{
}