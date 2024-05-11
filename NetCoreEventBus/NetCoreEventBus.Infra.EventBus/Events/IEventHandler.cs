namespace NetCoreEventBus.Infra.EventBus.Events;

public interface IEventHandler<in TEvent>
	where TEvent : Event
{
	Task HandleAsync(TEvent @event);
}