using HsnSoft.Base.MongoDB;
using MongoDB.Driver;
using NetCoreEventBus.Web.Shipment.Infra.Domain;

namespace NetCoreEventBus.Web.Shipment.Infra.Mongo;

public sealed class ShipmentMongoDbContext : BaseMongoDbContext
{
    public IMongoCollection<ShipmentEntity> Samples => Collection<ShipmentEntity>();

    public ShipmentMongoDbContext(IServiceProvider provider, IConfiguration configuration) : base(configuration.GetConnectionString("MongoDbConnection"), provider)
    {
    }
}