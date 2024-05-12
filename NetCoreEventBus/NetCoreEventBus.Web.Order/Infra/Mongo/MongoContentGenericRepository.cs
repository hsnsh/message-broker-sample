using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;
using NetCoreEventBus.Web.Order.Infra.Domain;

namespace NetCoreEventBus.Web.Order.Infra.Mongo;

public sealed class MongoContentGenericRepository<TEntity> : MongoGenericRepository<SampleMongoDbContext, TEntity, Guid>, IContentGenericRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    public MongoContentGenericRepository(IServiceProvider provider, SampleMongoDbContext dbContext) : base(provider, dbContext)
    {
       
    }
}