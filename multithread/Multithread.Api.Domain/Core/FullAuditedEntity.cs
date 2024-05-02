using Multithread.Api.Domain.Core.Audit;

namespace Multithread.Api.Domain.Core;

[Serializable]
public abstract class FullAuditedEntity : AuditedEntity, IFullAuditedObject
{
    public  bool IsDeleted { get; set; }

    public  Guid? DeleterId { get; set; }

    public  DateTime? DeletionTime { get; set; }
}

[Serializable]
public abstract class FullAuditedEntity<TKey> : AuditedEntity<TKey>, IFullAuditedObject
{
    public  bool IsDeleted { get; set; }

    public  Guid? DeleterId { get; set; }

    public  DateTime? DeletionTime { get; set; }

    protected FullAuditedEntity()
    {
    }

    protected FullAuditedEntity(TKey id)
        : base(id)
    {
    }
}