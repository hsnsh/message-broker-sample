using JetBrains.Annotations;

namespace GeneralLibrary.Base.Core;

public interface IBaseLogger : ISingletonDependency
{
    public void LogDebug([CanBeNull]string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogError(string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogWarning(string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogInformation(string messageTemplate, [ItemCanBeNull] params object[] args);
}