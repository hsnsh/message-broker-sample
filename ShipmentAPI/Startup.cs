using Hosting;

namespace ShipmentAPI;

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
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        AddEventBus(services);
    }

    public void Configure(IApplicationBuilder app)
    {
        if (!WebHostEnvironment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API");
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
                endpoints.MapGet("/", () => $"eShop Shipment Service | {WebHostEnvironment.EnvironmentName}:{Configuration.GetValue<string>("TestSettings")} | v1.0.0");
            }
        });

        UseEventBus(app);
    }

    private void AddEventBus(IServiceCollection services)
    {
        // services.AddKafkaEventBus(Configuration)
        //     .AddTransient<OrderStartedIntegrationEventHandler>();
    }

    private void UseEventBus(IApplicationBuilder app)
    {
        // var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        // eventBus.Subscribe<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();
        // eventBus.Subscribe<OrderStatusUpdatedIntegrationEvent, OrderStatusUpdatedIntegrationEventHandler>();
    }
}