using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared;
using NetCoreEventBus.Web.EventManager.Infra.Mongo;
using NetCoreEventBus.Web.EventManager.IntegrationEvents.EventHandlers;
using NetCoreEventBus.Web.EventManager.Services;

namespace NetCoreEventBus.Web.EventManager;

public class Startup
{
    private IConfiguration Configuration { get; }
    private IWebHostEnvironment WebHostEnvironment { get; }

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
        services.AddScoped<IEventErrorHandlerService, EventErrorHandlerService>();

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

        services.AddTransient<MessageBrokerErrorEtoHandler>();
    }

    private void ConfigureEventBusHandlers(IApplicationBuilder app)
    {
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        // Here you add the event handlers for each intergration event.
        eventBus.Subscribe<MessageBrokerErrorEto, MessageBrokerErrorEtoHandler>();
    }
}