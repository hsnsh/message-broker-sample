namespace Multithread.Api.Core.Security;

public interface ICurrentUser
{
    public Guid? Id { get; set; }
}

public class CurrentUser : ICurrentUser
{
    public Guid? Id { get; set; } = Guid.NewGuid();
}