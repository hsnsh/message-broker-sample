namespace GeneralLibrary.Base.EventBus;

public class SubscriptionInfo
{
    public Type HandlerType { get; }

    private SubscriptionInfo(Type handlerType)
    {
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
    }

    public static SubscriptionInfo Typed(Type handlerType) => new(handlerType);
}