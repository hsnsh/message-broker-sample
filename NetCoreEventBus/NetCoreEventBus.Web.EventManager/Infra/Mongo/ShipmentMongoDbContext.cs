using HsnSoft.Base.MongoDB;
using MongoDB.Driver;
using NetCoreEventBus.Web.EventManager.Infra.Domain;

namespace NetCoreEventBus.Web.EventManager.Infra.Mongo;

public sealed class ShipmentMongoDbContext : BaseMongoDbContext
{
    public IMongoCollection<FailedIntegrationEvent> Samples => Collection<FailedIntegrationEvent>();

    public ShipmentMongoDbContext(IServiceProvider provider, IConfiguration configuration) : base(configuration.GetConnectionString("MongoDbConnection"), provider)
    {
    }
}