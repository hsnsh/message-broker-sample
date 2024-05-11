namespace NetCoreEventBus.Infra.EventBus.Logging;

public interface IEventBusLogger : IBaseLogger
{
    public void EventBusInfoLog<T>(T t) where T : IEventBusLog;
    public void EventBusErrorLog<T>(T t) where T : IEventBusLog;
}