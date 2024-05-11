namespace NetCoreEventBus.Infra.EventBus.Subscriptions;

public class Subscription
{
	//public Type EventType { get; private set; }
	public Type HandlerType { get; private set; }

	public Subscription( Type handlerType)
	{
		//EventType = eventType;
		HandlerType = handlerType;
	}
}