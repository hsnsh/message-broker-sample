using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Multithread.Api.Auditing;
using Multithread.Api.MongoDb.ConfigurationMaps;

namespace Multithread.Api.MongoDb;

public static class MongoDbExtensions
{
    public static IServiceCollection AddMongoDatabaseConfiguration(this IServiceCollection services, Type assemblyReference, IConfiguration configuration)
    {
        services.AddBaseAuditingServiceCollection();
        
        MongoConfigure(assemblyReference);
        MongoClassMap.RegisterClassMaps();
     
        services.AddScoped<SampleMongoDbContext>();

        services.AddScoped(typeof(MongoRepository<,,>));

        return services;
    }

    private static void MongoConfigure(Type assemblyReference)
    {
        try
        {
            // MongoDB Guid support
            BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;
            // BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch { }

        // MongoDB New version
        var objectSerializer = new ObjectSerializer(type =>
        {
            if (type is null) throw new ArgumentNullException();
            var scope = assemblyReference.Namespace;
            return scope != null && type.FullName != null && (ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith(scope));
        });
        try
        {
            BsonSerializer.RegisterSerializer(objectSerializer);
        }
        catch { }

        // Conventions
        ConventionRegistry.Register("IgnoreConventions", new ConventionPack
        {
            new IgnoreExtraElementsConvention(true),
            // new IgnoreIfDefaultConvention(true),
            new IgnoreIfNullConvention(false)
        }, t => true);

        ConventionRegistry.Register("EnumStringConvention", new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String)
        }, t => true);
    }
}