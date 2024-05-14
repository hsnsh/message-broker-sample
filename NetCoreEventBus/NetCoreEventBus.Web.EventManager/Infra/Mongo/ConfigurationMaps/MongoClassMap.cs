using HsnSoft.Base.Domain.Entities;
using MongoDB.Bson.Serialization;
using NetCoreEventBus.Web.EventManager.Infra.Domain;

namespace NetCoreEventBus.Web.EventManager.Infra.Mongo.ConfigurationMaps;

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
        
        BsonClassMap.RegisterClassMap<FailedIntegrationEvent>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.Name).SetIsRequired(true);
        });
    }
}