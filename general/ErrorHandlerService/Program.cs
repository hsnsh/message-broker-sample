using ErrorHandlerService.EventHandlers;
using GeneralLibrary;
using GeneralLibrary.Base;
using GeneralLibrary.Base.EventBus;
using GeneralLibrary.Base.EventBus.Logging;
using GeneralLibrary.Base.EventBus.RabbitMQ;
using GeneralLibrary.Base.RabbitMQ;
using GeneralLibrary.Events;
using GeneralLibrary.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErrorHandlerService;

internal static class Program
{
    public static void Main(string[] args)
    {
        var configuration = GetConfiguration();

        var services = new ServiceCollection();

        services.AddOptions();

        // Add configuration objects
        services.AddSingleton<IEventBusLogger, DefaultEventBusLogger>();
        services.Configure<RabbitMqConnectionSettings>(configuration.GetSection("RabbitMq:Connection"));
        services.Configure<RabbitMqEventBusConfig>(configuration.GetSection("RabbitMq:EventBus"));
        services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();

        services.AddSingleton<IEventBus, EventBusRabbitMq>();

        services.AddTransient<MessageBrokerErrorEtoHandler>();

        var sp = services.BuildServiceProvider();

        IEventBus eventBus = sp.GetRequiredService<IEventBus>();

        // Subscribe all event handlers
        eventBus.Subscribe<MessageBrokerErrorEto, MessageBrokerErrorEtoHandler>();

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