using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Domain.Core;
using Multithread.Api.EntityFrameworkCore.Core;

namespace Multithread.Api.EntityFrameworkCore;

public interface IEfCoreRepository<TEntity> where TEntity : class, IEntity
{
    DbContext GetDbContext();

    DbSet<TEntity> GetDbSet();
}

public sealed class EfCoreRepository<TDbContext, TEntity> : IEfCoreRepository<TEntity>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity
{
    private readonly TDbContext _dbContext;
    private static readonly object DbResourceLock = new();

    public EfCoreRepository([NotNull] TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    DbContext IEfCoreRepository<TEntity>.GetDbContext() => GetDbContext();

    DbSet<TEntity> IEfCoreRepository<TEntity>.GetDbSet() => GetDbSet();

    private TDbContext GetDbContext() => _dbContext;
    private DbSet<TEntity> GetDbSet() => GetDbContext().Set<TEntity>();
    private void SaveChanges() => GetDbContext().SaveChanges();

    [ItemCanBeNull]
    public async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await GetDbSet().Where(predicate).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await GetDbSet().Where(predicate).ToListAsync(cancellationToken);
    }

    public Task<TEntity> InsertAsync(TEntity entity)
    {
        lock (DbResourceLock)
        {
            var inserted = GetDbSet().Add(entity).Entity;
            SaveChanges();

            return Task.FromResult(inserted);
        }
    }

    public Task InsertManyAsync(IEnumerable<TEntity> entities)
    {
        lock (DbResourceLock)
        {
            GetDbSet().AddRange(entities.ToArray());
            SaveChanges();
        }

        return Task.CompletedTask;
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        var context = GetDbContext();
        lock (DbResourceLock)
        {
            context.Attach(entity);
            var updated = context.Update(entity).Entity;
            SaveChanges();

            return Task.FromResult(updated);
        }
    }

    public Task UpdateManyAsync(IEnumerable<TEntity> entities)
    {
        lock (DbResourceLock)
        {
            GetDbSet().UpdateRange(entities);
            SaveChanges();
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity)
    {
        lock (DbResourceLock)
        {
            GetDbSet().Remove(entity);
            SaveChanges();
        }

        return Task.CompletedTask;
    }

    public Task DeleteManyAsync(IEnumerable<TEntity> entities)
    {
        var context = GetDbContext();
        lock (DbResourceLock)
        {
            context.RemoveRange(entities);
            SaveChanges();
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