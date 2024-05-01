using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Infrastructure.Domain;

namespace Multithread.Api.Infrastructure;

public sealed class SampleManager<TDbContext, TEntity> 
    where TDbContext : DbContext 
    where TEntity : class, IEntity
{
    private readonly TDbContext _dbContext;

    public SampleManager(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private DbSet<TEntity> GetDbSet() => _dbContext.Set<TEntity>();

    public async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await GetDbSet().Where(predicate).SingleOrDefaultAsync(cancellationToken);


    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await GetDbSet().Where(predicate).ToListAsync(cancellationToken);

    public async Task<TEntity> InsertAsync(TEntity entity) => (await GetDbSet().AddAsync(entity)).Entity;

    public async Task InsertManyAsync(IEnumerable<TEntity> entities) => await GetDbSet().AddRangeAsync(entities.ToArray());

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _dbContext.Attach(entity);
        return _dbContext.Update(entity).Entity;
    }

    public async Task UpdateManyAsync(IEnumerable<TEntity> entities) => GetDbSet().UpdateRange(entities);

    public async Task DeleteAsync(TEntity entity) => GetDbSet().Remove(entity);

    public async Task DeleteManyAsync(IEnumerable<TEntity> entities) => _dbContext.RemoveRange(entities);

    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await GetDbSet()
            .Where(predicate)
            .ToListAsync(cancellationToken);

        await DeleteManyAsync(entities);
    }

    public async Task DeleteDirectAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await GetDbSet().Where(predicate).ExecuteDeleteAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}