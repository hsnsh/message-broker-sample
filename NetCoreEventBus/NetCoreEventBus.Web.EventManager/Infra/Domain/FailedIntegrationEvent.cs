using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace NetCoreEventBus.Web.EventManager.Infra.Domain;

public sealed class FailedIntegrationEvent : FullAuditedEntity<Guid>
{
    [NotNull]
    public string Name { get;  set; }
    
    public string Desc { get;  set; }

    private FailedIntegrationEvent()
    {
        Name = string.Empty;
    }

    public FailedIntegrationEvent(Guid id, [NotNull] string name) : this()
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