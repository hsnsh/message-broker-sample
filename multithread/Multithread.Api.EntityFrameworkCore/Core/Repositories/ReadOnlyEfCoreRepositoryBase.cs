using System.Linq.Expressions;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore.Core.Repositories;

public abstract class ReadOnlyEfCoreRepositoryBase<TEntity, TKey> : ReadOnlyBasicRepositoryBase<TEntity, TKey>, IReadOnlyEfCoreRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    public IServiceProvider ServiceProvider { get; set; }

    protected ReadOnlyEfCoreRepositoryBase()
    {
    }

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