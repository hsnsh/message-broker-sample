using GeneralLibrary.Base.EventBus;
using GeneralLibrary.Events;
using Microsoft.AspNetCore;

namespace GeneralTestApi;

internal class Program
{
    private static readonly string Namespace = typeof(Startup).Namespace;
    private static readonly string AppName = Namespace?[(Namespace.IndexOf('.') + 1)..];

    public static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("Configuring web host ({0})...", AppName);
            var host = CreateHostBuilder(GetConfiguration(), args);

            using (var scope = host.Services.CreateScope())
            {
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
                for (var i = 0; i < 100; i++)
                {
                    await eventBus.PublishAsync(new OrderStartedEto(Guid.NewGuid()));
                }
            }

            Console.WriteLine("Starting web host ({0})...", AppName);
            await host.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Program terminated unexpectedly ({0})! {1}", AppName, ex);
            return 1;
        }
    }

    private static IWebHost CreateHostBuilder(IConfiguration configuration, string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .CaptureStartupErrors(false)
            .ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBufferSize = long.MaxValue;
                options.Limits.MaxRequestBodySize = long.MaxValue;
            })
            .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
            .UseStartup<Startup>()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            })
            .Build();

    private static IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json")
            .AddEnvironmentVariables()
            .Build();
}