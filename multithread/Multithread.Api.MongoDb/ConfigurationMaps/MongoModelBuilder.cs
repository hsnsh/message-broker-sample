using MongoDB.Bson.Serialization;
using Multithread.Api.Domain;
using Multithread.Api.Domain.Core;

namespace Multithread.Api.MongoDb.ConfigurationMaps;

public sealed class MongoModelBuilder
{
    public static void Configure()
    {
        BsonClassMap.RegisterClassMap<Entity<Guid>>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapIdMember(x => x.Id);
        });
        
        BsonClassMap.RegisterClassMap<SampleEntity>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.Name).SetIsRequired(true);
        });
    }
}