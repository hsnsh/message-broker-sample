using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NetCoreEventBus.Shared.Core.Serilog;

public class SerilogPersistentLogger : SerilogBaseLogger, IPersistentLogger
{
    public void PersistentInfoLog<T>(T t) where T : IPersistentLog => Logger.Log(LogLevel.Trace, JsonConvert.SerializeObject(t));

    public void PersistentErrorLog<T>(T t) where T : IPersistentLog => Logger.Log(LogLevel.Critical, JsonConvert.SerializeObject(t));
}