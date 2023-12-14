using Base.EventBus;
using Base.EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace NotificationProducer;

internal static class Program
{
    public static void Main(string[] args)
    {
        var configuration = GetConfiguration();

        using var loggerFactory = LoggerFactory.Create(static builder => builder.AddConsole());

        IEventBus _eventBus = new EventBusKafka(loggerFactory);
        ILogger logger = loggerFactory.CreateLogger("Program");

        while (true)
        {
            //produce email message
            var emailMessage = new EmailMessage
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