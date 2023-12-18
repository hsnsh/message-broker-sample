using Base.EventBus;
using Base.EventBus.Kafka;
using Base.EventBus.RabbitMQ;
using Base.RabbitMQ;
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
        services.Configure<KafkaEventBusSettings>(configuration.GetSection("Kafka:EventBus"));
        services.Configure<KafkaConnectionSettings>(configuration.GetSection("Kafka:Connection"));

        services.AddSingleton<IEventBus, EventBusKafka>(sp =>
        {
            // var busSettings = new KafkaEventBusSettings();
            // var conf= sp.GetRequiredService<IConfiguration>();
            // conf.Bind("Kafka:EventBus", busSettings);
            var busSettings = sp.GetRequiredService<IOptions<KafkaEventBusSettings>>();
            var connectionSettings = sp.GetRequiredService<IOptions<KafkaConnectionSettings>>();

            EventBusConfig config = new()
            {
                SubscriberClientAppName = busSettings.Value.ConsumerIdentifier, DefaultTopicName = string.Empty, ConnectionRetryCount = busSettings.Value.ConnectionRetryCount, EventNameSuffix = busSettings.Value.EventNameSuffix,
            };

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new EventBusKafka(sp, loggerFactory, config, $"{connectionSettings.Value.HostName}:{connectionSettings.Value.Port}");
        });

        // Add All Event Handlers
        services.AddEventHandlers();

        return services;
    }

    public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add our Config object so it can be injected
        services.Configure<RabbitMQEventBusSettings>(configuration.GetSection("RabbitMQ:EventBus"));
        services.Configure<RabbitMQConnectionSettings>(configuration.GetSection("RabbitMQ:Connection"));
    
        services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    
            var busSettings = sp.GetRequiredService<IOptions<RabbitMQEventBusSettings>>();
            var conSettings = sp.GetRequiredService<IOptions<RabbitMQConnectionSettings>>();
    
            var factory = new ConnectionFactory()
            {
                HostName = conSettings.Value.HostName, Port = conSettings.Value.Port, UserName = conSettings.Value.UserName, Password = conSettings.Value.Password,
            };
    
            return new RabbitMQPersistentConnection(factory, loggerFactory, busSettings.Value.ConnectionRetryCount);
        });
    
        services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
        {
            // var busSettings = new RabbitMQEventBusSettings();
            // var conf= sp.GetRequiredService<IConfiguration>();
            // conf.Bind("RabbitMQ:EventBus", busSettings);
            var busSettings = sp.GetRequiredService<IOptions<RabbitMQEventBusSettings>>();
    
            EventBusConfig config = new()
            {
                SubscriberClientAppName = busSettings.Value.ClientName, DefaultTopicName = busSettings.Value.ExchangeName, ConnectionRetryCount = busSettings.Value.ConnectionRetryCount, EventNameSuffix = busSettings.Value.EventNameSuffix,
            };
    
            var rabbitMqPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    
            return new EventBusRabbitMQ(sp, rabbitMqPersistentConnection, config, loggerFactory);
        });

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

    public static void UseEventBus(this IApplicationBuilder app)
    {
        var refType = typeof(IIntegrationEventHandler);
        var eventHandlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
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