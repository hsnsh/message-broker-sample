namespace NetCoreEventBus.Infra.EventBus.Events;

public interface IIntegrationEventHandler<in TEvent>
	where TEvent : Event
{
	Task HandleAsync(TEvent @event);
}