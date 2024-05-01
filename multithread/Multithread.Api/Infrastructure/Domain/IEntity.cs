namespace Multithread.Api.Infrastructure.Domain;

public interface IEntity
{
    object[] GetKeys();
}

public interface IEntity<out TKey> : IEntity
{
    TKey Id { get; }
}