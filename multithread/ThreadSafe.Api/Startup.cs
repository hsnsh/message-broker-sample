using Microsoft.EntityFrameworkCore;
using ThreadSafe.Api.Data;
using ThreadSafe.Api.Services;

namespace ThreadSafe.Api;

public class Startup
{
    private IConfiguration Configuration { get; }
    private IWebHostEnvironment WebHostEnvironment { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        WebHostEnvironment = environment;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddSingleton<NameService>();
        services.AddTransient<ISampleAppService, SampleAppService>();

        services.AddDbContext<BookContext>(options =>
            {
                options.UseNpgsql("Host=localhost;Port=35432;Database=BookDb;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;", sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    // sqlOptions.MigrationsAssembly(assemblyReference.Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(30, TimeSpan.FromSeconds(6), null);
                    sqlOptions.CommandTimeout(30);
                    // sqlOptions.MaxBatchSize(100);
                });
                options.EnableSensitiveDataLogging(false);
                // options.EnableThreadSafetyChecks(false);
            }
            , ServiceLifetime.Transient
        );

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        // Configure the HTTP request pipeline.
        if (WebHostEnvironment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

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
    }
}