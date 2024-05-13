using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Logging;

public sealed class DefaultEventBusLogger : DefaultBaseLogger, IEventBusLogger
{
    public void EventBusInfoLog<T>(T t) where T : IEventBusLog => Logger.Log(LogLevel.Trace, JsonConvert.SerializeObject(t));

    public void EventBusErrorLog<T>(T t) where T : IEventBusLog => Logger.Log(LogLevel.Critical, JsonConvert.SerializeObject(t));
}