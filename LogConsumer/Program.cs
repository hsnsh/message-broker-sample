using Base.EventBus;
using Base.EventBus.Kafka;
using Hosting;
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

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(sp =>
        {
            return LoggerFactory.Create(static builder => builder.SetMinimumLevel(LogLevel.Information).AddConsole());
        });

        // services.AddKafkaEventBus(configuration);
        services.AddRabbitMQEventBus(configuration);

        while (true)
        {
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