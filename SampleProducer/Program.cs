using Base.EventBus;
using Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared;

namespace ShipmentProducer;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = GetConfiguration();

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(sp => LoggerFactory.Create(static builder =>
            builder.SetMinimumLevel(LogLevel.Information).AddConsole()));

        // Add event bus instance
        services.AddEventBus(configuration);

        var sp = services.BuildServiceProvider();

        // Subscribe all event handlers
        sp.UseEventBus();

        IEventBus _eventBus = sp.GetRequiredService<IEventBus>();

        while (true)
        {
            await _eventBus.PublishAsync(new OrderStartedIntegrationEvent(Guid.NewGuid()));

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