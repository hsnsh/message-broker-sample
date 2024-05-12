using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace NetCoreEventBus.Web.Shipment.Infra.Domain;

public sealed class ShipmentEntity : FullAuditedEntity<Guid>
{
    [NotNull]
    public string Name { get;  set; }
    
    public string Desc { get;  set; }

    private ShipmentEntity()
    {
        Name = string.Empty;
    }

    public ShipmentEntity(Guid id, [NotNull] string name) : this()
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