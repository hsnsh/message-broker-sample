using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;
using Multithread.Api.Domain;

namespace Multithread.Api.EntityFrameworkCore;

public sealed class EfCoreContentGenericRepository<TEntity> : EfCoreGenericRepository<SampleEfCoreDbContext, TEntity, Guid>, IContentGenericRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    public EfCoreContentGenericRepository(IServiceProvider provider, SampleEfCoreDbContext dbContext) : base(provider, dbContext)
    {
    }
}