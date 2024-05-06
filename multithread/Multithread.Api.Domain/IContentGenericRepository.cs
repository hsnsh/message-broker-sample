using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.Domain.Core.Repositories;

namespace Multithread.Api.Domain;

public interface IContentGenericRepository<TEntity> : IGenericRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
}