using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multithread.Api.Domain;

namespace Multithread.Api.EntityFrameworkCore;

public static class EntityFrameworkExtensions
{
    public static IServiceCollection AddEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBaseAuditingServiceCollection();
        services.AddBaseDataServiceCollection();

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
                // options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
                options.EnableSensitiveDataLogging(true);
                // options.EnableThreadSafetyChecks(false);
            }
            , ServiceLifetime.Transient
        );

        // services.AddTransient(typeof(IEfCoreGenericRepository<,,>), typeof(EfCoreGenericRepository<,,>));
        services.AddTransient(typeof(IContentGenericRepository<>), typeof(EfCoreContentGenericRepository<>));
        // services.AddScoped(typeof(ThreadLockEfCoreRepository<,>));

        return services;
    }
}