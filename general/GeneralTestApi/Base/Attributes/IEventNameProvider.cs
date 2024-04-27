namespace GeneralTestApi.Base.Attributes;

public interface IEventNameProvider
{
    string GetName(Type eventType);
}