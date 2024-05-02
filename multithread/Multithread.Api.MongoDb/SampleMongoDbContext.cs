using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Multithread.Api.Domain;
using Multithread.Api.MongoDb.Core;

namespace Multithread.Api.MongoDb;

public sealed class SampleMongoDbContext : BaseMongoDbContext
{
    public IMongoCollection<SampleEntity> Samples => Set<SampleEntity>();

    public SampleMongoDbContext(IOptions<MongoDbSettings> settings) : base(settings)
    {
        
    }
}