using System.Linq.Expressions;
using JetBrains.Annotations;
using MongoDB.Driver;
using Multithread.Api.Domain.Core;
using Multithread.Api.MongoDb.Core;

namespace Multithread.Api.MongoDb;

public sealed class MongoRepository<TDbContext, TEntity>
    where TDbContext : MongoDbContext
    where TEntity : class, IEntity
{
    private readonly TDbContext _dbContext;
    private readonly FindOptions<TEntity> _findOptions;

    public MongoRepository([NotNull] TDbContext dbContext)
    {
        _dbContext = dbContext;
        _findOptions = new FindOptions<TEntity>()
        {
            MaxAwaitTime = _dbContext.WaitQueueTimeout,
            MaxTime = _dbContext.WaitQueueTimeout
        };
    }

    private IMongoCollection<TEntity> GetDbSet() => _dbContext?.Set<TEntity>();

    [ItemCanBeNull]
    public async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetDbSet().FindAsync(predicate, _findOptions, cancellationToken);

        return asyncCursor != null ? await asyncCursor.FirstOrDefaultAsync(cancellationToken) : null;
    }

    public async Task InsertAsync(TEntity entity)
    {
        await GetDbSet().InsertOneAsync(entity, new InsertOneOptions() { BypassDocumentValidation = false });
    }

    public async Task<int> DeleteDirect(Expression<Func<TEntity, bool>> predicate)
    {
        var a = await GetDbSet().DeleteOneAsync(predicate);

        return a.IsAcknowledged ? (int)a.DeletedCount : 0;
    }
}