using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Multithread.Api.Domain;

namespace Multithread.Api.MongoDb;

public sealed class SampleMongoDbContext : BaseMongoDbContext
{
    public IMongoCollection<SampleEntity> Samples => Collection<SampleEntity>();

    public SampleMongoDbContext(IServiceProvider provider, IConfiguration configuration) : base(provider, configuration.GetConnectionString("SampleMongoDb"))
    {
    }
}