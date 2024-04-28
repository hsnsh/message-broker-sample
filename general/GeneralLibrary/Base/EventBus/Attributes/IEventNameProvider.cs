namespace GeneralLibrary.Base.EventBus.Attributes;

public interface IEventNameProvider
{
    string GetName(Type eventType);
}