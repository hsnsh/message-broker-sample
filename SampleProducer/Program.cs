using Hosting;
using Hosting.Events;
using HsnSoft.Base.EventBus.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        services.AddMicroserviceEventBus(configuration);

        var sp = services.BuildServiceProvider();

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