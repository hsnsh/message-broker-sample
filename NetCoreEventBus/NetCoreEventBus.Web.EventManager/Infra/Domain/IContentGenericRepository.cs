using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace NetCoreEventBus.Web.EventManager.Infra.Domain;

public interface IContentGenericRepository<TEntity> : IGenericRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
}