using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.MongoDb.Core.Repositories;

public interface IMongoRepository<TDbContext, TEntity>
    where TDbContext : BaseMongoDbContext
    where TEntity : class, IEntity
{
    IMongoCollection<TEntity> GetCollection();

    IMongoQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IMongoQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IMongoQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}

public interface IReadOnlyMongoRepository<TDbContext, TEntity, in TKey> : IReadOnlyBasicRepository<TEntity, TKey>, IMongoRepository<TDbContext, TEntity>
    where TDbContext : BaseMongoDbContext
    where TEntity : class, IEntity<TKey>
{
}

public interface IManagerMongoRepository<TDbContext, TEntity, in TKey> : IManagerBasicRepository<TEntity, TKey>, IMongoRepository<TDbContext, TEntity>
    where TDbContext : BaseMongoDbContext
    where TEntity : class, IEntity<TKey>
{
}