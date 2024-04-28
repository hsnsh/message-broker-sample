using JetBrains.Annotations;

namespace GeneralLibrary.Base.Core;

public interface ITraceAccesor
{
    [CanBeNull]
    string GetCorrelationId();

    [CanBeNull]
    string GetUserId();

    [CanBeNull]
    string[] GetRoles();

    [CanBeNull]
    string GetChannel();
}