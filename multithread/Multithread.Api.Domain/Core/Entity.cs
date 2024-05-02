namespace Multithread.Api.Domain.Core;

[Serializable]
public abstract class Entity : IEntity
{
    protected Entity()
    {
    }

    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Keys = {string.Join(", ", GetKeys())}";
    }

    public abstract object[] GetKeys();
}

[Serializable]
public abstract class Entity<TKey> : Entity, IEntity<TKey>
{
    public TKey Id { get; set; }

    protected Entity()
    {
    }

    protected Entity(TKey id)
    {
        Id = id;
    }

    public override object[] GetKeys()
    {
        return new object[] { Id };
    }

    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }
}