using Microsoft.AspNetCore;
using ThreadSafe.Api.Workers;

namespace ThreadSafe.Api;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(GetConfiguration(), args);

            using (var scope = host.Services.CreateScope())
            {
                // var context = scope.ServiceProvider.GetRequiredService<BookContext>();
                // if (! await context.Database.EnsureCreatedAsync())
                // {
                //    await context.Database.MigrateAsync();
                // }
            }

            await host.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
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
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices(x =>
            {
                x.AddHostedService<InsertWorkerService>();
                x.AddHostedService<DeleteWorkerService>();
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