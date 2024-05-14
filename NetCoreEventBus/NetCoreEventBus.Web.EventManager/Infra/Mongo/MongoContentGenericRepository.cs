using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;
using NetCoreEventBus.Web.EventManager.Infra.Domain;

namespace NetCoreEventBus.Web.EventManager.Infra.Mongo;

public sealed class MongoContentGenericRepository<TEntity> : MongoGenericRepository<ShipmentMongoDbContext, TEntity, Guid>, IContentGenericRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    public MongoContentGenericRepository(IServiceProvider provider, ShipmentMongoDbContext dbContext) : base(provider, dbContext)
    {
       
    }
}