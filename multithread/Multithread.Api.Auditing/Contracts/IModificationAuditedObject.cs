namespace Multithread.Api.Auditing.Contracts;

public interface IModificationAuditedObject : IHasModificationTime
{
    Guid? LastModifierId { get; set; }
}