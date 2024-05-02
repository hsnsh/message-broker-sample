using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Multithread.Api.Domain;
using Multithread.Api.MongoDb.Core;

namespace Multithread.Api.MongoDb;

public sealed class SampleMongoDbContext : BaseMongoDbContext
{
    public IMongoCollection<SampleEntity> Samples => Collection<SampleEntity>();

    public SampleMongoDbContext(IConfiguration configuration) : base(configuration.GetConnectionString("SampleMongoDb"))
    {
    }
}