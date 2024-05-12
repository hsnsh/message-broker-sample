using HsnSoft.Base.MongoDB;
using MongoDB.Driver;
using NetCoreEventBus.Web.Order.Infra.Domain;

namespace NetCoreEventBus.Web.Order.Infra.Mongo;

public sealed class OrderMongoDbContext : BaseMongoDbContext
{
    public IMongoCollection<OrderEntity> Samples => Collection<OrderEntity>();

    public OrderMongoDbContext(IServiceProvider provider, IConfiguration configuration) : base(configuration.GetConnectionString("MongoDbConnection"), provider)
    {
    }
}