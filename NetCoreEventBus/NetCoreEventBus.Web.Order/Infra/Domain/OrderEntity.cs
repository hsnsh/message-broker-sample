using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace NetCoreEventBus.Web.Order.Infra.Domain;

public sealed class OrderEntity : FullAuditedEntity<Guid>
{
    [NotNull]
    public string Name { get;  set; }
    
    public string Desc { get;  set; }

    private OrderEntity()
    {
        Name = string.Empty;
    }

    public OrderEntity(Guid id, [NotNull] string name) : this()
    {
        Id = id;
        SetName(name);
    }

    internal void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name), "name is not valid");

        Name = name;
    }
}