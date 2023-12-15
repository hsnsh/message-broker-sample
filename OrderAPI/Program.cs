using Microsoft.AspNetCore;

namespace OrderAPI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(GetConfiguration(), args).RunAsync();
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
}