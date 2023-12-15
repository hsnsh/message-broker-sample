using Base.EventBus;
using Base.EventBus.Kafka;
using Base.EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace ShipmentProducer;

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

        var sp = services.BuildServiceProvider();

        IEventBus _eventBus = sp.GetRequiredService<IEventBus>();

        while (true)
        {
            _eventBus.Publish(new OrderStartedIntegrationEvent() { OrderId = Guid.NewGuid() });

            var result = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(result) && result.ToLower().Equals("q")) break;
        }
    }

    private static IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
}