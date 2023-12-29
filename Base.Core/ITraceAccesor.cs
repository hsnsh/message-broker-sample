namespace Base.Core;

public interface ITraceAccesor
{
    string? GetCorrelationId();

    string? GetUserId();

    string[] GetRoles();
}