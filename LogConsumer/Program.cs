using Base.EventBus;
using Base.EventBus.Kafka;
using LogConsumer.handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        services.AddTransient<EmailMessageIntegrationEventHandler>();
        
        services.AddSingleton<ILoggerFactory>(sp =>
        {
            return LoggerFactory.Create(static builder => builder.AddConsole());
        });

        services.AddSingleton<IEventBus, EventBusKafka>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new EventBusKafka(sp, loggerFactory);
        });

        var sp = services.BuildServiceProvider();

        IEventBus _eventBus = sp.GetRequiredService<IEventBus>();

        _eventBus.Subscribe<EmailMessageIntegrationEvent, EmailMessageIntegrationEventHandler>();
    }

    private static IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
}