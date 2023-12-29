using Hosting;
using Hosting.Events;
using HsnSoft.Base.EventBus.Abstractions;
using LogConsumer.EventHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LogConsumer;

internal static class Program
{
    public static void Main(string[] args)
    {
        var configuration = GetConfiguration();

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(sp => LoggerFactory.Create(static builder =>
            builder.SetMinimumLevel(LogLevel.Information).AddConsole()));

        // Add event bus instance
        services.AddMicroserviceEventBus(configuration);

        var sp = services.BuildServiceProvider();

        IEventBus _eventBus = sp.GetRequiredService<IEventBus>();

        // Subscribe all event handlers
        _eventBus.Subscribe<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();
        _eventBus.Subscribe<OrderShippingStartedIntegrationEvent, OrderShippingStartedIntegrationEventHandler>();
        _eventBus.Subscribe<ShipmentStartedIntegrationEvent, ShipmentStartedIntegrationEventHandler>();
        _eventBus.Subscribe<OrderShippingCompletedIntegrationEvent, OrderShippingCompletedIntegrationEventHandler>();


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