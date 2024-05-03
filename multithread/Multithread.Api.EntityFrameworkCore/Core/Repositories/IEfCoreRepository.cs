using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore.Core.Repositories;

public interface IEfCoreRepository<TEntity> where TEntity : class, IEntity
{
    DbContext GetDbContext();

    DbSet<TEntity> GetDbSet();

    IQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}

public interface IReadOnlyEfCoreRepository<TEntity, in TKey> : IReadOnlyBasicRepository<TEntity, TKey>, IEfCoreRepository<TEntity>
    where TEntity : class, IEntity<TKey>
{
}

public interface IManagerEfCoreRepository<TEntity, in TKey> : IManagerBasicRepository<TEntity, TKey>, IEfCoreRepository<TEntity>
    where TEntity : class, IEntity<TKey>
{
}