using System.Linq.Dynamic.Core;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetCoreEventBus.Web.Order.Infra.Domain;

namespace NetCoreEventBus.Web.Order.Infra;

public sealed class EfCoreOrderGenericRepository<TEntity> : EfCoreGenericRepository<OrderEfCoreDbContext, TEntity, Guid>, IOrderGenericRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    public EfCoreOrderGenericRepository(IServiceProvider provider, OrderEfCoreDbContext dbContext) : base(provider, dbContext)
    {
    }


    public async Task<List<TEntity>> GetFilterListAsync(string filterText = null,
        string sorting = null, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var queryable = includeDetails
            ? WithDetails(DefaultPropertySelector?.ToArray())
            : GetQueryable();

        var query = ApplyFilter(queryable);

        return await query
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? "Id asc" : sorting)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    private IQueryable<TEntity> ApplyFilter(
        IQueryable<TEntity> query
    )
    {
        return query;
    }
}