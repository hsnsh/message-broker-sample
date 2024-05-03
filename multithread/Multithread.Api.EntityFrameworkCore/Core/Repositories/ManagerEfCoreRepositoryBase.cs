using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore.Core.Repositories;

public abstract class ManagerEfCoreRepositoryBase<TEntity, TKey> : ManagerBasicRepositoryBase<TEntity, TKey>, IManagerEfCoreRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    public IServiceProvider ServiceProvider { get; set; }

    protected ManagerEfCoreRepositoryBase()
    {
    }
    
    public abstract DbContext GetDbContext();

    public abstract DbSet<TEntity> GetDbSet();

    public virtual IQueryable<TEntity> WithDetails()
    {
        return GetQueryable();
    }

    public virtual IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return GetQueryable();
    }

    public abstract IQueryable<TEntity> GetQueryable();
}