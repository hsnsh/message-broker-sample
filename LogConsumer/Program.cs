using Base.EventBus;
using Base.EventBus.Kafka;
using LogConsumer.EventHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace LogConsumer;

internal static class Program
{
    public static void Main(string[] args)
    {
        var configuration = GetConfiguration();

        using var loggerFactory = LoggerFactory.Create(static builder => builder.AddConsole());
        ILogger logger = loggerFactory.CreateLogger("Program");

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(sp =>
        {
            return LoggerFactory.Create(static builder => builder.AddConsole());
        });

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

        services.AddTransient<OrderStartedIntegrationEventHandler>();
        services.AddTransient<OrderStatusShippedIntegrationEventHandler>();
        services.AddTransient<ShipmentStartedIntegrationEventHandler>();
        services.AddTransient<ShipmentCompletedIntegrationEventHandler>();

        var sp = services.BuildServiceProvider();

        IEventBus _eventBus = sp.GetRequiredService<IEventBus>();

        _eventBus.Subscribe<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();
        _eventBus.Subscribe<OrderShippingStartedIntegrationEvent, OrderStatusShippedIntegrationEventHandler>();
        _eventBus.Subscribe<ShipmentStartedIntegrationEvent, ShipmentStartedIntegrationEventHandler>();
        _eventBus.Subscribe<OrderShippingCompletedIntegrationEvent, ShipmentCompletedIntegrationEventHandler>();
    }

    private static IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
}