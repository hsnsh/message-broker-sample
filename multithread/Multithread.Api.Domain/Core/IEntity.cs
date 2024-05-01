namespace Multithread.Api.Domain.Core;

public interface IEntity
{
    object[] GetKeys();
}

public interface IEntity<out TKey> : IEntity
{
    TKey Id { get; }
}