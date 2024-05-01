using Microsoft.EntityFrameworkCore;
using Multithread.Api.Application;
using Multithread.Api.Infrastructure;

namespace Multithread.Api;

public sealed class Startup
{
    private IConfiguration Configuration { get; }
    private IWebHostEnvironment WebHostEnvironment { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        WebHostEnvironment = environment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddDbContext<SampleDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("SampleDb"), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(Program).Assembly.GetName().Name);
                });
                // options.EnableSensitiveDataLogging(true);
                // options.EnableThreadSafetyChecks(false);
            }
        );

        services.AddScoped(typeof(SampleManager<,>));

        services.AddScoped<ISampleAppService, SampleAppService>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IHostApplicationLifetime hostApplicationLifetime)
    {
        if (WebHostEnvironment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            if (!WebHostEnvironment.IsProduction())
            {
                endpoints.MapDefaultControllerRoute();
            }
            else
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", () => $"Test Service | {WebHostEnvironment.EnvironmentName} | v1.0.0");
            }
        });

        hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);
    }

    private void OnShutdown()
    {
        Console.WriteLine("Application stopping...");
    }
}