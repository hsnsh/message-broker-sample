using GeneralLibrary.Base.Core;

namespace GeneralLibrary.Base.EventBus.Logging;

public interface IEventBusLogger : IBaseLogger
{
    public void EventBusInfoLog<T>(T t) where T : IEventBusLog;
    public void EventBusErrorLog<T>(T t) where T : IEventBusLog;
}