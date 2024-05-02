using Multithread.Api.Domain.Core.Audit;

namespace Multithread.Api.Domain.Core;

[Serializable]
public abstract class AuditedEntity : CreationAuditedEntity, IAuditedObject
{
    public DateTime? LastModificationTime { get; set; }

    public Guid? LastModifierId { get; set; }
}

[Serializable]
public abstract class AuditedEntity<TKey> : CreationAuditedEntity<TKey>, IAuditedObject
{
    public DateTime? LastModificationTime { get; set; }

    public Guid? LastModifierId { get; set; }

    protected AuditedEntity()
    {
    }

    protected AuditedEntity(TKey id)
        : base(id)
    {
    }
}