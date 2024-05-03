using System.Linq.Expressions;
using Multithread.Api.Domain.Core.Entities;

namespace Multithread.Api.Domain.Core.Repositories;

public abstract class ManagerEfCoreRepositoryBase<TEntity, TKey> : ManagerBasicRepositoryBase<TEntity, TKey>, IManagerEfCoreRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    public virtual Task<IQueryable<TEntity>> WithDetailsAsync()
    {
        return GetQueryableAsync();
    }

    public virtual Task<IQueryable<TEntity>> WithDetailsAsync(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return GetQueryableAsync();
    }

    public abstract Task<IQueryable<TEntity>> GetQueryableAsync();
}