using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using HsnSoft.Base.EventBus.SubManagers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NetCoreEventBus.Shared;

public static class ServiceCollectionExtensions
{
    public static void AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventBusLogger, DefaultEventBusLogger>();
        services.Configure<RabbitMqConnectionSettings>(configuration.GetSection("RabbitMq:Connection"));
        services.Configure<RabbitMqEventBusConfig>(configuration.GetSection("RabbitMq:EventBus"));
        services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();

        services.AddSingleton<IEventBus, EventBusRabbitMq>(sp =>
        {
            // var factory = sp.GetService<IServiceScopeFactory>();
            // var persistentConnection = sp.GetService<IRabbitMqPersistentConnection>();
            // var subscriptionManager = sp.GetService<IEventBusSubscriptionManager>();
            // var eventBusSettings = sp.GetService<IOptions<RabbitMqEventBusConfig>>();
            // var conSettings = sp.GetService<IOptions<RabbitMqConnectionSettings>>();
            // var logger = sp.GetService<IEventBusLogger>();

            // return new EventBusRabbitMq(factory, persistentConnection, conSettings?.Value, subscriptionManager, eventBusSettings, logger);
            return new EventBusRabbitMq(sp);
        });
    }
}