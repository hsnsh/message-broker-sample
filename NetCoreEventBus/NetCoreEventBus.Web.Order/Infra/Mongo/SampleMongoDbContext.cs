using HsnSoft.Base.MongoDB;
using MongoDB.Driver;
using NetCoreEventBus.Web.Order.Infra.Domain;

namespace NetCoreEventBus.Web.Order.Infra.Mongo;

public sealed class SampleMongoDbContext : BaseMongoDbContext
{
    public IMongoCollection<SampleEntity> Samples => Collection<SampleEntity>();

    public SampleMongoDbContext(IServiceProvider provider, IConfiguration configuration) : base(configuration.GetConnectionString("SampleMongoDb"), provider)
    {
    }
}