using System.Linq.Expressions;
using JetBrains.Annotations;
using MongoDB.Driver;
using Multithread.Api.Core;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.MongoDb.Core;

namespace Multithread.Api.MongoDb;

public sealed class MongoRepository<TDbContext, TEntity, TKey>
    where TDbContext : BaseMongoDbContext
    where TEntity : class, IEntity<TKey>
{
    private readonly TDbContext _dbContext;
    private readonly FindOptions<TEntity> _findOptions;

    public MongoRepository([NotNull] TDbContext dbContext)
    {
        _dbContext = dbContext;
        _findOptions = new FindOptions<TEntity>
        {
            MaxAwaitTime = _dbContext.ClientWaitQueueTimeout,
            MaxTime = _dbContext.ClientWaitQueueTimeout
        };
    }

    private IMongoCollection<TEntity> GetDbSet() => _dbContext?.Collection<TEntity>();

    [ItemCanBeNull]
    public async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetDbSet().FindAsync(predicate, _findOptions, cancellationToken);

        return asyncCursor != null ? await asyncCursor.FirstOrDefaultAsync(cancellationToken) : null;
    }

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        await _dbContext.AddEntityCommandAsync(async _ =>
        {
            _dbContext.ApplyEntityTrackingChanges(entity, MongoCommandState.Added);
            await GetDbSet().InsertOneAsync(entity, new InsertOneOptions { BypassDocumentValidation = false });
            return entity;
        });

        return await _dbContext.SaveChangesAsync() > 0 ? entity : null;
    }

    public async Task<bool> DeleteByIdAsync(TKey id)
    {
        var obj = await (await GetDbSet().FindAsync(Builders<TEntity>.Filter.Eq("_id", id))).SingleAsync();
        return await DeleteAsync(obj);
    }

    public async Task<bool> DeleteAsync(TEntity entity)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Soft delete
            return await DeleteManyAsync(new List<TEntity> { entity });
        }

        // Hard Delete
        await _dbContext.AddEntityCommandAsync(async _ =>
        {
            var deleteResult = await GetDbSet().DeleteOneAsync(Builders<TEntity>.Filter.Eq("_id", entity.Id));
            return deleteResult.IsAcknowledged ? (int)deleteResult.DeletedCount : 0;
        });

        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteManyAsync(List<TEntity> entities)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Soft delete
            foreach (var entity in entities)
            {
                await _dbContext.AddEntityCommandAsync(async _ =>
                {
                    _dbContext.ApplyEntityTrackingChanges(entity, MongoCommandState.Deleted);
                    var replaceResult = await GetDbSet().ReplaceOneAsync(Builders<TEntity>.Filter.Eq("_id", entity.Id), entity);
                    return replaceResult.IsAcknowledged ? (int)replaceResult.ModifiedCount : 0;
                });
            }

            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Hard Delete
        var idList = entities.Select(x => x.Id).ToList();
        await _dbContext.AddEntityCommandAsync(async _ =>
        {
            var deleteResult = await GetDbSet().DeleteManyAsync(x => idList.Contains(x.Id));
            return deleteResult.IsAcknowledged ? (int)deleteResult.DeletedCount : 0;
        });

        return await _dbContext.SaveChangesAsync() > 0;
    }
}