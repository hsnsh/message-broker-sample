using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Multithread.Api.MongoDb.Core;

namespace Multithread.Api.MongoDb;

public sealed class SampleMongoDbContext : MongoDbContext
{
    public SampleMongoDbContext(IOptions<MongoDbSettings> settings) : base(settings)
    {
    }

    public SampleMongoDbContext(IMongoClient mongoClient, string databaseName) : base(mongoClient, databaseName)
    {
        Console.WriteLine(Database);
    }
}