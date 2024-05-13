using HsnSoft.Base.EventBus.Logging;
using NetCoreEventBus.Shared.Core.Serilog;

namespace NetCoreEventBus.Shared;

public sealed class SerilogEventBusLogger : SerilogBaseLogger, IEventBusLogger
{
    public void EventBusInfoLog<T>(T t) where T : IEventBusLog
    {
        throw new NotImplementedException();
    }

    public void EventBusErrorLog<T>(T t) where T : IEventBusLog
    {
        throw new NotImplementedException();
    }
}