using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hosting;
using HsnSoft.Base.AspNetCore.Tracing;
using ShipmentAPI.EventHandlers;
using ShipmentAPI.Services;

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

    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.ConfigureMicroserviceHost();

        services.AddMicroserviceEventBus(Configuration, typeof(EventHandlersAssemblyMarker).Assembly);

        services.AddSingleton<IShipmentService, ShipmentService>();
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        var container = new ContainerBuilder();
        container.Populate(services);

        return new AutofacServiceProvider(container.Build());
    }

    public void Configure(IApplicationBuilder app, IHostApplicationLifetime hostApplicationLifetime)
    {
        if (!WebHostEnvironment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API");
            });
        }

        app.UseCorrelationId();
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

        // Subscribe all event handlers
        app.UseEventBus(typeof(EventHandlersAssemblyMarker).Assembly);
        
        hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);
    }
    
    private void OnShutdown()
    {
        Console.WriteLine("Application stopping...");
    }
}