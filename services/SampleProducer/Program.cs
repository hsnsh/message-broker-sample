using Hosting;
using Hosting.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ShipmentProducer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = GetConfiguration();

        var services = new ServiceCollection();

        // Add event bus instance
        services.AddMicroserviceEventBus(configuration, typeof(EventHandlersAssemblyMarker).Assembly);

        var sp = services.BuildServiceProvider();

        // var logger = sp.GetService<IEventBusLogger>();
        //
        // logger.LogDebug("LogDebug log message");
        // logger.LogError("LogError log message");
        // logger.LogWarning("LogWarning log message");
        // logger.LogInformation("LogInformation log message");
        //
        // logger.EventBusErrorLog(new ProduceMessageLogModel(Guid.NewGuid().ToString(), "", "", DateTimeOffset.UtcNow, null, ""));
        // logger.EventBusInfoLog(new ProduceMessageLogModel(Guid.NewGuid().ToString(), "", "", DateTimeOffset.UtcNow, null, ""));

        var eventBus = sp.GetRequiredService<IEventBus>();

        // // TEST SCENARIO 1
        // while (true)
        // {
        //     await _eventBus.PublishAsync(new OrderStartedEto(Guid.NewGuid()));
        //
        //     var result = Console.ReadLine();
        //     if (!string.IsNullOrWhiteSpace(result) && result.ToLower().Equals("q")) break;
        // }

        // TEST SCENARIO 2
        for (var i = 1; i <= 1000; i++)
        {
            await eventBus.PublishAsync(new OrderStartedEto(Guid.NewGuid()));
            Console.WriteLine("Published: {0}", +i);
        }

        Console.WriteLine("SAMPLE PUBLISHER TERMINATED");
    }

    private static IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
}