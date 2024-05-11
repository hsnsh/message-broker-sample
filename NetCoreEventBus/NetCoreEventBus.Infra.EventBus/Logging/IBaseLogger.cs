using JetBrains.Annotations;

namespace NetCoreEventBus.Infra.EventBus.Logging;

public interface IBaseLogger : ISingletonDependency
{
    public void LogDebug([CanBeNull]string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogError(string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogWarning(string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogInformation(string messageTemplate, [ItemCanBeNull] params object[] args);
}