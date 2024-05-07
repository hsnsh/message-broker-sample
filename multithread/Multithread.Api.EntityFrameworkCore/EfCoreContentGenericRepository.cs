using Multithread.Api.Domain;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.EntityFrameworkCore.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore;

public sealed class EfCoreContentGenericRepository<TEntity> : EfCoreGenericRepository<SampleEfCoreDbContext, TEntity, Guid>, IContentGenericRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    public EfCoreContentGenericRepository(IServiceProvider provider, SampleEfCoreDbContext dbContext) : base(provider, dbContext)
    {
    }
}