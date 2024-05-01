using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Infrastructure.Domain;

namespace Multithread.Api.Infrastructure;

public sealed class SampleManager<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : class, IEntity
{
    private readonly TDbContext _dbContext;
    private static readonly object DbResourceLock = new();

    public SampleManager([NotNull] TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private DbSet<TEntity> GetDbSet() => _dbContext?.Set<TEntity>();
    private void SaveChanges() => _dbContext.SaveChanges();

    [ItemCanBeNull]
    public async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await GetDbSet().Where(predicate).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await GetDbSet().Where(predicate).ToListAsync(cancellationToken);
    }

    public Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false)
    {
        lock (DbResourceLock)
        {
            var inserted = GetDbSet().Add(entity).Entity;
            if (autoSave)
            {
                SaveChanges();
            }

            return Task.FromResult(inserted);
        }
    }

    public Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false)
    {
        lock (DbResourceLock)
        {
            GetDbSet().AddRange(entities.ToArray());
            if (autoSave)
            {
                SaveChanges();
            }
        }

        return Task.CompletedTask;
    }

    public Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false)
    {
        lock (DbResourceLock)
        {
            _dbContext.Attach(entity);
            var updated = _dbContext.Update(entity).Entity;
            if (autoSave)
            {
                SaveChanges();
            }

            return Task.FromResult(updated);
        }
    }

    public Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false)
    {
        lock (DbResourceLock)
        {
            GetDbSet().UpdateRange(entities);
            if (autoSave)
            {
                SaveChanges();
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, bool autoSave = false)
    {
        lock (DbResourceLock)
        {
            GetDbSet().Remove(entity);
            if (autoSave)
            {
                SaveChanges();
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false)
    {
        lock (DbResourceLock)
        {
            _dbContext.RemoveRange(entities);
            if (autoSave)
            {
                SaveChanges();
            }
        }

        return Task.CompletedTask;
    }

    public int DeleteDirect(Expression<Func<TEntity, bool>> predicate)
    {
        lock (DbResourceLock)
        {
            return GetDbSet().Where(predicate).ExecuteDelete();
        }
    }
}