namespace Multithread.Api.Domain.Core;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}