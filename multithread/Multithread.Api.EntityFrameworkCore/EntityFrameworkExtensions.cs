using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Multithread.Api.EntityFrameworkCore;

public static class EntityFrameworkExtensions
{
    public static IServiceCollection AddEfCoreDatabaseConfiguration(this IServiceCollection services, Type assemblyReference, IConfiguration configuration)
    {
        services.AddDbContext<SampleEfCoreDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("SampleDb"), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(assemblyReference.Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(30, TimeSpan.FromSeconds(6), null);
                    sqlOptions.MaxBatchSize(100);
                });
                options.EnableSensitiveDataLogging(false);
                // options.EnableThreadSafetyChecks(false);
            }
        );

        services.AddScoped(typeof(EfCoreRepository<,>));


        return services;
    }
}