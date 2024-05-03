using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Core;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore.Core.Repositories;

public class EfCoreRepository<TDbContext, TEntity, TKey> : ManagerBasicRepositoryBase<TEntity, TKey>, IManagerEfCoreRepository<TEntity, TKey>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
    private readonly TDbContext _dbContext;

    public List<Expression<Func<TEntity, object>>> DefaultPropertySelector = null;

    public EfCoreRepository(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbContext GetDbContext() => _dbContext;

    public DbSet<TEntity> GetDbSet() => GetDbContext().Set<TEntity>();

    public IQueryable<TEntity> WithDetails()
    {
        if (DefaultPropertySelector == null)
        {
            return GetQueryable();
        }

        return WithDetails(DefaultPropertySelector.ToArray());
    }

    public IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return IncludeDetails(GetQueryable(), propertySelectors);
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return GetDbSet().AsQueryable();
    }

    public override async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await WithDetails()
                .Where(predicate)
                .SingleOrDefaultAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet()
                .Where(predicate)
                .SingleOrDefaultAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<TEntity> FindAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await WithDetails().OrderBy(e => e.Id).FirstOrDefaultAsync(e => e.Id.Equals(id), GetCancellationToken(cancellationToken))
            : await GetDbSet().FindAsync(new object[] { id }, GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await WithDetails().Where(predicate).ToListAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet().Where(predicate).ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await WithDetails().ToListAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet().ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await GetDbSet().LongCountAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var queryable = includeDetails
            ? WithDetails()
            : GetDbSet();

        return await queryable
            .OrderByIf<TEntity, IQueryable<TEntity>>(!sorting.IsNullOrWhiteSpace(), sorting)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        CheckAndSetId(entity);

        var savedEntity = (await GetDbSet().AddAsync(entity, GetCancellationToken(cancellationToken))).Entity;

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        return savedEntity;
    }

    public override async Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var entityArray = entities.ToArray();
        cancellationToken = GetCancellationToken(cancellationToken);

        foreach (var entity in entityArray)
        {
            CheckAndSetId(entity);
        }

        await GetDbSet().AddRangeAsync(entityArray, cancellationToken);

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var dbContext = GetDbContext();
        dbContext.Attach(entity);

        var updatedEntity = dbContext.Update(entity).Entity;

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        return updatedEntity;
    }

    public override async Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        GetDbSet().UpdateRange(entities);

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        GetDbSet().Remove(entity);

        var resultCount = await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));

        return resultCount > 0;
    }

    public override async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await GetDbSet()
            .Where(predicate)
            .ToListAsync(GetCancellationToken(cancellationToken));

        await DeleteManyAsync(entities, cancellationToken);

        var resultCount = await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));

        return resultCount > 0;
    }

    public override async Task DeleteManyAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        await GetDbSet().Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        GetDbContext().RemoveRange(entities);

        await GetDbContext().SaveChangesAsync(cancellationToken);
    }

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await GetDbContext().SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<TEntity> IncludeDetails(IQueryable<TEntity> query, Expression<Func<TEntity, object>>[] propertySelectors)
    {
        if (!propertySelectors.IsNullOrEmpty())
        {
            foreach (var propertySelector in propertySelectors)
            {
                query = query.Include(propertySelector);
            }
        }

        return query;
    }

    protected void CheckAndSetId(TEntity entity)
    {
        if (entity is IEntity<Guid> entityWithGuidId)
        {
            TrySetGuidId(entityWithGuidId);
        }
    }

    protected virtual void TrySetGuidId(IEntity<Guid> entity)
    {
        if (entity.Id != default)
        {
            return;
        }

        entity.Id = Guid.NewGuid();
    }
}