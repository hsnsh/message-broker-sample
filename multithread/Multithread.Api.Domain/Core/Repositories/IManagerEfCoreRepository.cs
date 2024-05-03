using System.Linq.Expressions;
using Multithread.Api.Domain.Core.Entities;

namespace Multithread.Api.Domain.Core.Repositories;

public interface IManagerEfCoreRepository<TEntity, in TKey> : IManagerBasicRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<IQueryable<TEntity>> WithDetailsAsync(); //TODO: CancellationToken

    Task<IQueryable<TEntity>> WithDetailsAsync(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    Task<IQueryable<TEntity>> GetQueryableAsync(); //TODO: CancellationToken
}