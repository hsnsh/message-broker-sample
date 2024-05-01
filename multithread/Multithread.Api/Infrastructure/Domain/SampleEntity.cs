using JetBrains.Annotations;

namespace Multithread.Api.Infrastructure.Domain;

public sealed class SampleEntity : Entity<Guid>
{
    [NotNull]
    public string Name { get; internal set; }

    private SampleEntity()
    {
        Name = string.Empty;
    }

    internal SampleEntity(Guid id, [NotNull] string name) : this()
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