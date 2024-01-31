using System.Reflection;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Kafka;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.Kafka;
using HsnSoft.Base.RabbitMQ;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hosting;

public static class MicroserviceHostExtensions
{
    public static IServiceCollection ConfigureMicroserviceHost(this IServiceCollection services)
    {
        services.AddControllers();

        return services;
    }

    public static IServiceCollection AddMicroserviceEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        //  services.AddKafkaEventBus(configuration);
        services.AddRabbitMQEventBus(configuration);

        // Add All Event Handlers
        services.AddEventHandlers();

        return services;
    }

    private static void AddKafkaEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration objects
        services.Configure<KafkaConnectionSettings>(configuration.GetSection("Kafka:Connection"));
        services.Configure<KafkaEventBusConfig>(configuration.GetSection("Kafka:EventBus"));

        // Add event bus instances
        services.AddHttpContextAccessor();
        services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
        services.AddSingleton<IEventBusLogger, DefaultEventBusLogger>();
        services.AddSingleton<IEventBus, EventBusKafka>(sp => new EventBusKafka(sp));
    }

    private static void AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration objects
        services.Configure<RabbitMQConnectionSettings>(configuration.GetSection("RabbitMQ:Connection"));
        services.Configure<RabbitMQEventBusConfig>(configuration.GetSection("RabbitMQ:EventBus"));

        // Add event bus instances
        services.AddHttpContextAccessor();
        services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
        services.AddSingleton<IEventBusLogger, DefaultEventBusLogger>();
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

    public static void UseEventBus(this IApplicationBuilder app, Assembly assembly)
    {
        var refType = typeof(IIntegrationEventHandler);
        var eventHandlerTypes = assembly.GetTypes()
            .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }).ToList();

        if (eventHandlerTypes is not { Count: > 0 }) return;

        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        foreach (var eventHandlerType in eventHandlerTypes)
        {
            var eventType = eventHandlerType.GetInterfaces().First(x => x.IsGenericType).GenericTypeArguments[0];

            eventBus.Subscribe(eventType, eventHandlerType);
        }
    }
}