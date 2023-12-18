using Base.EventBus;
using Base.EventBus.Kafka;
using Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace ShipmentProducer;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = GetConfiguration();

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(sp =>
        {
            return LoggerFactory.Create(static builder => builder.SetMinimumLevel(LogLevel.Trace).AddConsole());
        });

        // services.AddKafkaEventBus(configuration);
        services.AddRabbitMQEventBus(configuration);

        var sp = services.BuildServiceProvider();

        // Subscribe all event handlers
        sp.UseEventBus();
        
        IEventBus _eventBus = sp.GetRequiredService<IEventBus>();

        while (true)
        {
            await _eventBus.PublishAsync(new OrderStartedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid()));

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