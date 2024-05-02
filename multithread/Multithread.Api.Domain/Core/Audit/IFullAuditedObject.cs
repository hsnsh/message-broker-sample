namespace Multithread.Api.Domain.Core.Audit;

public interface IFullAuditedObject : IAuditedObject, ISoftDelete
{
    DateTime? DeletionTime { get; set; }
    Guid? DeleterId { get; set; }
}