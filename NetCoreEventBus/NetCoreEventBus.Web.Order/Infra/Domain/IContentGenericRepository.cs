using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace NetCoreEventBus.Web.Order.Infra.Domain;

public interface IContentGenericRepository<TEntity> : IGenericRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
}