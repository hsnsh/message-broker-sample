using JetBrains.Annotations;
using Multithread.Api.Core;
using Multithread.Api.Domain.Core.Entities.Audit;

namespace Multithread.Api.Domain;

public sealed class SampleEntity : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; set; }

    [NotNull]
    public string Name { get; internal set; }

    private SampleEntity()
    {
        Name = string.Empty;
    }

    public SampleEntity(Guid id, [NotNull] string name) : this()
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