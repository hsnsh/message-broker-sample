using HsnSoft.Base.Domain.Entities;
using MongoDB.Bson.Serialization;
using Multithread.Api.Domain;

namespace Multithread.Api.MongoDb.ConfigurationMaps;

public sealed class MongoClassMap
{
    public static void RegisterClassMaps()
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