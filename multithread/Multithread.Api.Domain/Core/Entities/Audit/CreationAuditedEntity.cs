using Multithread.Api.Auditing.Contracts;

namespace Multithread.Api.Domain.Core.Entities.Audit;

[Serializable]
public abstract class CreationAuditedEntity : Entity, ICreationAuditedObject
{
    public DateTime CreationTime { get;  set; }

    public Guid? CreatorId { get;  set; }
}

[Serializable]
public abstract class CreationAuditedEntity<TKey> : Entity<TKey>, ICreationAuditedObject
{
    protected CreationAuditedEntity()
    {
    }

    protected CreationAuditedEntity(TKey id)
        : base(id)
    {
    }

    public DateTime CreationTime { get;  set; }

    public Guid? CreatorId { get;  set; }
}