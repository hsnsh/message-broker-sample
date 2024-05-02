using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Multithread.Api.MongoDb.ConfigurationMaps;
using Multithread.Api.MongoDb.Core;

namespace Multithread.Api.MongoDb;

public static class MongoDbExtensions
{
    public static IServiceCollection AddMongoDatabaseConfiguration(this IServiceCollection services, Type assemblyReference, IConfiguration configuration)
    {
        MongoConfigure(assemblyReference);
        MongoClassMap.RegisterClassMaps();

        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        services.AddScoped<SampleMongoDbContext>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>();
            return new SampleMongoDbContext(settings);
        });

        services.AddScoped(typeof(MongoRepository<,>));


        return services;
    }

    private static void MongoConfigure(Type assemblyReference)
    {
        // MongoDB Guid support
        BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;
        // BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        // MongoDB New version 
        var objectSerializer = new ObjectSerializer(type =>
        {
            if (type is null) throw new ArgumentNullException();
            var scope = assemblyReference.Namespace;
            return scope != null && type.FullName != null && (ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith(scope));
        });
        BsonSerializer.RegisterSerializer(objectSerializer);

        // Conventions
        var ignorePack = new ConventionPack
        {
            new IgnoreExtraElementsConvention(true),
            new IgnoreIfDefaultConvention(true)
        };
        ConventionRegistry.Register("IgnoreConventions", ignorePack, t => true);

        var enumPack = new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String)
        };
        ConventionRegistry.Register("EnumStringConvention", enumPack, t => true);
    }
}