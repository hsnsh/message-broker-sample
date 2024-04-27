namespace GeneralLibrary.Base.Attributes;

public interface IEventNameProvider
{
    string GetName(Type eventType);
}