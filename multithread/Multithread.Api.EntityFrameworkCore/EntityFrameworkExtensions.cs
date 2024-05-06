using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multithread.Api.Auditing;
using Multithread.Api.EntityFrameworkCore.Core.Repositories;

namespace Multithread.Api.EntityFrameworkCore;

public static class EntityFrameworkExtensions
{
    public static IServiceCollection AddEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBaseAuditingServiceCollection();

        services.AddDbContext<SampleEfCoreDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("SampleDb"), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    // sqlOptions.MigrationsAssembly(assemblyReference.Assembly.GetName().Name);
                    sqlOptions.MigrationsAssembly(typeof(SampleEfCoreDbContext).Namespace);
                    sqlOptions.EnableRetryOnFailure(30, TimeSpan.FromSeconds(6), null);
                    sqlOptions.MaxBatchSize(100);
                });
                options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
                options.EnableSensitiveDataLogging(true);
                // options.EnableThreadSafetyChecks(false);
            }
            , ServiceLifetime.Transient
        );

        services.AddTransient(typeof(IReadOnlyEfCoreRepository<,,>), typeof(EfCoreRepository<,,>));
        services.AddTransient(typeof(IManagerEfCoreRepository<,,>), typeof(EfCoreRepository<,,>));
        // services.AddScoped(typeof(ThreadLockEfCoreRepository<,>));

        return services;
    }
}