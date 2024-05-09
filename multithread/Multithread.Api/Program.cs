using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Application;
using Multithread.Api.EntityFrameworkCore;
using Multithread.Api.Workers;

namespace Multithread.Api;

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
                var dbContext = scope.ServiceProvider.GetRequiredService<SampleEfCoreDbContext>();
                try
                {
                    if (dbContext.Database.CanConnectAsync().GetAwaiter().GetResult())
                    {
                        if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
                        {
                            // apply pending migrations
                            await dbContext.Database.MigrateAsync();
                        }
                    }
                    else
                    {
                        // first creation
                        await dbContext.Database.MigrateAsync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                // var sampleAppService = scope.ServiceProvider.GetRequiredService<ISampleAppService>();
                // for (var i = 1; i <= 100; i++)
                // {
                //     await sampleAppService.InsertOperation(i);
                //     Console.WriteLine("Published: {0}", i);
                // }
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
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices(x =>
            {
                x.AddHostedService<InsertWorkerService>();
                // x.AddHostedService<UpdateWorkerService>();
                // x.AddHostedService<DeleteWorkerService>();
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