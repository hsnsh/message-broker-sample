using Hosting;
using Hosting.Events;
using HsnSoft.Base.EventBus;
using LogConsumer.EventHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogConsumer;

internal static class Program
{
    public static void Main(string[] args)
    {
        var configuration = GetConfiguration();

        var services = new ServiceCollection();

        // Add event bus instance
        services.AddMicroserviceEventBus(configuration, typeof(EventHandlersAssemblyMarker).Assembly);

        var sp = services.BuildServiceProvider();

        IEventBus eventBus = sp.GetRequiredService<IEventBus>();

        // Subscribe all event handlers
        eventBus.Subscribe<OrderStartedEto, OrderStartedIntegrationEventHandler>();
        eventBus.Subscribe<OrderShippingStartedEto, OrderShippingStartedIntegrationEventHandler>();
        eventBus.Subscribe<ShipmentStartedEto, ShipmentStartedIntegrationEventHandler>();
        eventBus.Subscribe<OrderShippingCompletedEto, OrderShippingCompletedIntegrationEventHandler>();

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