namespace Multithread.Api.Auditing.Contracts;

public interface IMayHaveCreator
{
    Guid? CreatorId { get; set; }
}
