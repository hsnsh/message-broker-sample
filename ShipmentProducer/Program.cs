using Base.EventBus;
using Base.EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        services.AddSingleton<IEventBus, EventBusKafka>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new EventBusKafka(sp, loggerFactory);
        });

        var sp = services.BuildServiceProvider();

        IEventBus _eventBus = sp.GetRequiredService<IEventBus>();

        while (true)
        {
            //produce email message
            var emailMessage = new EmailMessageIntegrationEvent
            {
                Content = DateTime.Now.ToString("yyyyMMdd HH:mm:ss zzz"),
                Subject = "Contoso Retail Daily News",
                To = "all@contosoretail.com.tr"
            };
            _eventBus.Publish(emailMessage);

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