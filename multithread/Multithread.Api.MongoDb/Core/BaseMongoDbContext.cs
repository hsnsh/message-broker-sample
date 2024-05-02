using System.Reflection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Multithread.Api.Domain.Core;

namespace Multithread.Api.MongoDb.Core;

public abstract class BaseMongoDbContext : IScopedDependency
{
    protected IMongoDatabase Database { get; }
  
    public TimeSpan WaitQueueTimeout { get; private set; }

    protected BaseMongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var clientSettings = CreateClientSettings(settings);
        Database = new MongoClient(clientSettings).GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<TDocument> Set<TDocument>() where TDocument : class, IEntity
    {
        return Database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
    }

    private MongoClientSettings CreateClientSettings(IOptions<MongoDbSettings> settings)
    {
        ThreadPool.GetMaxThreads(out var maxWt, out var _);
        var clientSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
        clientSettings.MaxConnectionPoolSize = maxWt * 2;

        var queryExecutionMaxSeconds = settings.Value.QueryExecutionMaxSeconds > 0
            ? TimeSpan.FromSeconds(settings.Value.QueryExecutionMaxSeconds)
            : TimeSpan.FromSeconds(60);

        // In version 2.19, MongoDB team upgraded to LinqProvider.V3, rolling back to V2 until LinQ is stable...
        // https://www.mongodb.com/community/forums/t/issue-with-2-18-to-2-19-nuget-upgrade-of-mongodb-c-driver/211894/2
        clientSettings.LinqProvider = LinqProvider.V2;
        clientSettings.WaitQueueTimeout = queryExecutionMaxSeconds;

        WaitQueueTimeout = queryExecutionMaxSeconds;

        return clientSettings;
    }
    
    private static string GetCollectionName(MemberInfo entityType)
        => ((BsonCollectionAttribute)entityType.GetCustomAttributes(typeof(BsonCollectionAttribute), true)
               .FirstOrDefault())?.CollectionName
           ?? entityType.Name;
}