using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Infrastructure.Domain;

namespace Multithread.Api.Infrastructure;

public sealed class SampleManager<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : class, IEntity
{
    private readonly TDbContext _dbContext;
    private static readonly object DbResourceLock = new object();

    public SampleManager(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private DbSet<TEntity> GetDbSet() => _dbContext.Set<TEntity>();

    public async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await GetDbSet().Where(predicate).SingleOrDefaultAsync(cancellationToken);


    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await GetDbSet().Where(predicate).ToListAsync(cancellationToken);

    public async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var inserted = (await GetDbSet().AddAsync(entity, cancellationToken)).Entity;
        if (autoSave)
        {
            await SaveChangesAsync(cancellationToken);
        }

        return inserted;
    }

    public async Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        await GetDbSet().AddRangeAsync(entities.ToArray());
        if (autoSave)
        {
            await SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbContext.Attach(entity);
        var updated = _dbContext.Update(entity).Entity;
        if (autoSave)
        {
            await SaveChangesAsync(cancellationToken);
        }

        return updated;
    }

    public async Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        GetDbSet().UpdateRange(entities);
        if (autoSave)
        {
            await SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        GetDbSet().Remove(entity);
        if (autoSave)
        {
            await SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbContext.RemoveRange(entities);
        if (autoSave)
        {
            await SaveChangesAsync(cancellationToken);
        }
    }

    public Task<int> DeleteDirectAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        lock (DbResourceLock)
        {
            return Task.FromResult(GetDbSet().Where(predicate).ExecuteDeleteAsync(cancellationToken).GetAwaiter().GetResult());
        }
    }

    private Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        lock (DbResourceLock)
        {
            return Task.FromResult(_dbContext.SaveChangesAsync(cancellationToken).GetAwaiter().GetResult());
        }
    }
}