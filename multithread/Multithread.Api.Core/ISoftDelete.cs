namespace Multithread.Api.Core;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}