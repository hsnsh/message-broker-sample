using Multithread.Api.Domain;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.MongoDb.Core.Repositories;

namespace Multithread.Api.MongoDb;

public sealed class MongoContentGenericRepository<TEntity> : MongoGenericRepository<SampleMongoDbContext, TEntity, Guid>, IContentGenericRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    public MongoContentGenericRepository(SampleMongoDbContext dbContext) : base(dbContext)
    {
    }
}