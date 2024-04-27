using GeneralTestApi.Base;
using GeneralTestApi.EventHandlers;

namespace GeneralTestApi;

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

        services.AddOptions();

        // Add configuration objects
        services.Configure<RabbitMqConnectionSettings>(Configuration.GetSection("RabbitMQ:Connection"));
        services.Configure<RabbitMqEventBusConfig>(Configuration.GetSection("RabbitMQ:EventBus"));
        services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();


        services.AddSingleton<IEventBus, EventBusRabbitMq>();

        services.AddTransient<OrderStartedIntegrationEventHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IHostApplicationLifetime hostApplicationLifetime)
    {
        if (!WebHostEnvironment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Test API");
            });
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

        IEventBus eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        // Subscribe all event handlers
        eventBus.Subscribe<OrderStartedEto, OrderStartedIntegrationEventHandler>();

        hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);
    }

    private void OnShutdown()
    {
        Console.WriteLine("Application stopping...");
    }
}