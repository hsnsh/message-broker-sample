using Microsoft.EntityFrameworkCore;
using Multithread.Api.Domain.Core.Entities;

namespace Multithread.Api.EntityFrameworkCore.Core.Repositories;

public interface IEfCoreRepository<TEntity, in TKey> : IManagerEfCoreRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    DbContext GetDbContext();

    DbSet<TEntity> GetDbSet();
}