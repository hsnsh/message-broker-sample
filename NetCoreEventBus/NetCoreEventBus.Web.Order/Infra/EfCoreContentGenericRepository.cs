using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;
using NetCoreEventBus.Web.Order.Infra.Domain;

namespace NetCoreEventBus.Web.Order.Infra;

public sealed class EfCoreContentGenericRepository<TEntity> : EfCoreGenericRepository<OrderEfCoreDbContext, TEntity, Guid>, IContentGenericRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    public EfCoreContentGenericRepository(IServiceProvider provider, OrderEfCoreDbContext dbContext) : base(provider, dbContext)
    {
    }
}