using Multithread.Api.Core;

namespace Multithread.Api.Auditing.Contracts;

public interface IHasDeletionTime : ISoftDelete
{
    DateTime? DeletionTime { get; set; }
}