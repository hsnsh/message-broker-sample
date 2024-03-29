using Hosting;
using HsnSoft.Base.AspNetCore.Tracing;

namespace OrderAPI;

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

        services.AddMicroserviceEventBus(Configuration);

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
                endpoints.MapGet("/", () => $"eShop Order Service | {WebHostEnvironment.EnvironmentName}:{Configuration.GetValue<string>("TestSettings")} | v1.0.0");
            }
        });

        // Subscribe all event handlers
        app.UseEventBus(typeof(Startup).Assembly);
    }
}