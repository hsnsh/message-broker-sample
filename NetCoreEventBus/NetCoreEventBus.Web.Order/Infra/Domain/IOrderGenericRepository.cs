using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace NetCoreEventBus.Web.Order.Infra.Domain;

public interface IOrderGenericRepository<TEntity> : IGenericRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
    Task<List<TEntity>> GetFilterListAsync(
        [CanBeNull] string filterText = null,
        string sorting = null,
        bool includeDetails = false,
        CancellationToken cancellationToken = default
    );
}