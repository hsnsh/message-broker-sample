namespace Multithread.Api.Domain.Core.Audit;

public interface ICreationAuditedObject
{
    DateTime CreationTime { get; set; }
    Guid? CreatorId { get;set;  }
}