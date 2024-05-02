namespace Multithread.Api.Domain.Core.Audit;

public interface IAuditedObject : ICreationAuditedObject
{
    DateTime? LastModificationTime { get; set; }
    Guid? LastModifierId { get; set; }
}