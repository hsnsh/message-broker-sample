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

    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafkaEventBus(configuration);
        // services.AddRabbitMQEventBus(configuration);

        // Add All Event Handlers
        services.AddEventHandlers();

        return services;
    }

    private static void AddKafkaEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration objects
        services.Configure<KafkaConnectionSettings>(configuration.GetSection("Kafka:Connection"));
        services.Configure<EventBusConfig>(configuration.GetSection("Kafka:EventBus"));

        // Add event bus instances
        services.AddSingleton<IEventBus, EventBusKafka>(sp => new EventBusKafka(sp));
    }

    private static void AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration objects
        services.Configure<RabbitMQConnectionSettings>(configuration.GetSection("RabbitMQ:Connection"));
        services.Configure<EventBusConfig>(configuration.GetSection("RabbitMQ:EventBus"));

        // Add event bus instances
        services.AddSingleton<IRabbitMQPersistentConnection>(sp => new RabbitMQPersistentConnection(sp));
        services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp => new EventBusRabbitMQ(sp));
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