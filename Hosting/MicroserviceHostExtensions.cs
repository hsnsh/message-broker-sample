using Base.EventBus;
using Base.EventBus.Kafka;
using Base.EventBus.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Hosting;

public static class MicroserviceHostExtensions
{
    public static IServiceCollection ConfigureMicroserviceHost(this IServiceCollection services)
    {
        services.AddControllers();

        return services;
    }

    public static IServiceCollection AddKafkaEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add our Config object so it can be injected
        services.Configure<KafkaConnectionSettings>(configuration.GetSection("Kafka:Connection"));
        services.Configure<EventBusConfig>(configuration.GetSection("Kafka:EventBus"));

        services.AddSingleton<IEventBus, EventBusKafka>(sp =>
        {
            // var busSettings = new KafkaEventBusSettings();
            // var conf= sp.GetRequiredService<IConfiguration>();
            // conf.Bind("Kafka:EventBus", busSettings);
            var connectionSettings = sp.GetRequiredService<IOptions<KafkaConnectionSettings>>();
            var busSettings = sp.GetRequiredService<IOptions<EventBusConfig>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new EventBusKafka(sp, loggerFactory, connectionSettings.Value, busSettings.Value);
        });

        // Add All Event Handlers
        services.AddEventHandlers();

        return services;
    }

    public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration objects
        services.Configure<EventBusConfig>(configuration.GetSection("RabbitMQ:EventBus"));
        services.Configure<RabbitMQConnectionSettings>(configuration.GetSection("RabbitMQ:Connection"));

        // Add event bus instances
        services.AddSingleton<IRabbitMQPersistentConnection>(sp => new RabbitMQPersistentConnection(sp));
        services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp => new EventBusRabbitMQ(sp));

        // Add All Event Handlers
        services.AddEventHandlers();

        return services;
    }

    private static void AddEventHandlers(this IServiceCollection services)
    {
        var refType = typeof(IIntegrationEventHandler);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false });

        foreach (var type in types.ToList())
        {
            services.AddTransient(type);
        }
    }

    public static void UseEventBus(this IServiceProvider sp)
    {
        var refType = typeof(IIntegrationEventHandler);
        var eventHandlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }).ToList();

        if (eventHandlerTypes is not { Count: > 0 }) return;
        var eventBus = sp.GetRequiredService<IEventBus>();

        foreach (var eventHandlerType in eventHandlerTypes)
        {
            var eventType = eventHandlerType.GetInterfaces().First(x => x.IsGenericType).GenericTypeArguments[0];

            eventBus.Subscribe(eventType, eventHandlerType);
        }
    }
}