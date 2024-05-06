using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.EntityFrameworkCore.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore;

public interface IContentGenericRepository<TEntity> : IEfCoreGenericRepository<SampleEfCoreDbContext, TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
}