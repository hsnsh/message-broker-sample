using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.MongoDb.Core.Repositories;

public interface IMongoGenericRepository<out TDbContext, TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TDbContext : BaseMongoDbContext
    where TEntity : class, IEntity<TKey>
{
    TDbContext GetDbContext();

    IMongoCollection<TEntity> GetCollection();

    IMongoQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IMongoQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IMongoQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}