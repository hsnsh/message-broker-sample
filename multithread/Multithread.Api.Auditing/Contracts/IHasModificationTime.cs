namespace Multithread.Api.Auditing.Contracts;

public interface IHasModificationTime
{
    DateTime? LastModificationTime { get; set; }
}
