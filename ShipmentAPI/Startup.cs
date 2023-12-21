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
        services.ConfigureMicroserviceHost();

        services.AddEventBus(Configuration);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
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

        app.UseCorrelationId();
        
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

        // Subscribe all event handlers
        app.ApplicationServices.UseEventBus();
    }
}