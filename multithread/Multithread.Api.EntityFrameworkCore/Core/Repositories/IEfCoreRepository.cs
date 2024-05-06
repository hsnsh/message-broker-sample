using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore.Core.Repositories;

public interface IEfCoreRepository<TDbContext, TEntity>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity
{
    TDbContext GetDbContext();

    DbSet<TEntity> GetDbSet();

    IQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}

public interface IReadOnlyEfCoreRepository<TDbContext, TEntity, in TKey> : IReadOnlyBasicRepository<TEntity, TKey>, IEfCoreRepository<TDbContext, TEntity>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
}

public interface IManagerEfCoreRepository<TDbContext, TEntity, in TKey> : IManagerBasicRepository<TEntity, TKey>, IEfCoreRepository<TDbContext, TEntity>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
}