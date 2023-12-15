using Base.EventBus;
using Base.EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hosting;

public static class MicroserviceHostExtensions
{
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
                SubscriberClientAppName = busSettings.Value.ConsumerGroupId, DefaultTopicName = string.Empty, ConnectionRetryCount = busSettings.Value.ConnectionRetryCount, EventNameSuffix = busSettings.Value.EventNameSuffix,
            };

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new EventBusKafka(sp, loggerFactory, config, $"{connectionSettings.Value.HostName}:{connectionSettings.Value.Port}");
        });

        return services;
    }

    // public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    // {
    //     // Add our Config object so it can be injected
    //     services.Configure<RabbitMQEventBusSettings>(configuration.GetSection("RabbitMQ:EventBus"));
    //     services.Configure<RabbitMQConnectionSettings>(configuration.GetSection("RabbitMQ:Connection"));
    //
    //     services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
    //     {
    //         var logger = sp.GetRequiredService<ILogger<RabbitMQPersistentConnection>>();
    //
    //         var busSettings = sp.GetRequiredService<IOptions<RabbitMQEventBusSettings>>();
    //         var conSettings = sp.GetRequiredService<IOptions<RabbitMQConnectionSettings>>();
    //
    //         var factory = new ConnectionFactory()
    //         {
    //             HostName = conSettings.Value.HostName, Port = conSettings.Value.Port, UserName = conSettings.Value.UserName, Password = conSettings.Value.Password,
    //         };
    //
    //         return new RabbitMQPersistentConnection(factory, logger, busSettings.Value.ConnectionRetryCount);
    //     });
    //
    //     services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
    //     {
    //         // var busSettings = new RabbitMQEventBusSettings();
    //         // var conf= sp.GetRequiredService<IConfiguration>();
    //         // conf.Bind("RabbitMQ:EventBus", busSettings);
    //         var busSettings = sp.GetRequiredService<IOptions<RabbitMQEventBusSettings>>();
    //
    //         EventBusConfig config = new()
    //         {
    //             SubscriberClientAppName = busSettings.Value.ClientName, DefaultTopicName = busSettings.Value.ExchangeName, ConnectionRetryCount = busSettings.Value.ConnectionRetryCount, EventNameSuffix = busSettings.Value.EventNameSuffix,
    //         };
    //
    //         var rabbitMqPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
    //         var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();
    //
    //         return new EventBusRabbitMQ(sp, rabbitMqPersistentConnection, config, logger);
    //     });
    //
    //     return services;
    // }
}