using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Shipment.Infra.Mongo;
using NetCoreEventBus.Web.Shipment.IntegrationEvents.EventHandlers;
using NetCoreEventBus.Web.Shipment.Services;

namespace NetCoreEventBus.Web.Shipment;

public class Startup
{
    public IConfiguration Configuration { get; }
    public IWebHostEnvironment WebHostEnvironment { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        Configuration = configuration;
        WebHostEnvironment = webHostEnvironment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        if (WebHostEnvironment.IsDevelopment())
        {
            services.AddSwaggerGen();
        }

        services.AddSingleton<IBaseLogger,DefaultBaseLogger>();
        
        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IShipmentService, ShipmentService>();

        services.AddMongoDatabaseConfiguration(Configuration);
        
        // Here we configure the event bus
        ConfigureEventBusDependencies(services);
    }

    public void Configure(IApplicationBuilder app)
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
            endpoints.MapControllers();
        });

        // Here we configure event handler subscriptions that the application  has to process
        ConfigureEventBusHandlers(app);
    }

    private void ConfigureEventBusDependencies(IServiceCollection services)
    {
        services.AddRabbitMQEventBus(Configuration);

        services.AddTransient<OrderShippingStartedEtoHandler>();
        services.AddTransient<ShipmentStartedEtoHandler>();
    }

    private void ConfigureEventBusHandlers(IApplicationBuilder app)
    {
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        // Here you add the event handlers for each intergration event.
        eventBus.Subscribe<OrderShippingStartedEto, OrderShippingStartedEtoHandler>();
        eventBus.Subscribe<ShipmentStartedEto, ShipmentStartedEtoHandler>();
    }
}