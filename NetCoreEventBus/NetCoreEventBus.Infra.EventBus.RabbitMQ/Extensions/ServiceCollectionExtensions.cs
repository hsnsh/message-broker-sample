using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCoreEventBus.Infra.EventBus.Bus;
using NetCoreEventBus.Infra.EventBus.Logging;
using NetCoreEventBus.Infra.EventBus.RabbitMQ.Bus;
using NetCoreEventBus.Infra.EventBus.RabbitMQ.Connection;
using NetCoreEventBus.Infra.EventBus.Subscriptions;

namespace NetCoreEventBus.Infra.EventBus.RabbitMQ.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventBusLogger, DefaultEventBusLogger>();
        services.Configure<RabbitMqConnectionSettings>(configuration.GetSection("RabbitMq:Connection"));
        services.Configure<RabbitMqEventBusConfig>(configuration.GetSection("RabbitMq:EventBus"));
        services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();

        services.AddSingleton<IEventBus, RabbitMQEventBus>(sp =>
        {
            var factory = sp.GetService<IServiceScopeFactory>();
            var persistentConnection = sp.GetService<IRabbitMqPersistentConnection>();
            var subscriptionManager = sp.GetService<IEventBusSubscriptionManager>();
            var eventBusSettings = sp.GetService<IOptions<RabbitMqEventBusConfig>>();
            var logger = sp.GetService<ILogger<RabbitMQEventBus>>();

            return new RabbitMQEventBus(factory, persistentConnection, subscriptionManager, eventBusSettings, logger);
        });
    }
}