namespace Multithread.Api.Auditing.Contracts;

public interface IDeletionAuditedObject : IHasDeletionTime
{
    Guid? DeleterId { get; set; }
}