using JetBrains.Annotations;
using Multithread.Api.Domain.Core;

namespace Multithread.Api.Domain;

public sealed class SampleEntity : FullAuditedEntity<Guid>
{
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