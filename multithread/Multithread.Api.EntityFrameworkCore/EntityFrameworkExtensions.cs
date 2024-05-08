using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                // options.EnableThreadSafetyChecks(false);
                options.UseNpgsql(configuration.GetConnectionString("SampleDb"), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(SampleEfCoreDbContext).Namespace);
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(6), null);
                    sqlOptions.CommandTimeout(30000);
                    sqlOptions.MaxBatchSize(100);
                });
                options.EnableSensitiveDataLogging(false);
                // options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
                // options.EnableSensitiveDataLogging(true);
            }
            , ServiceLifetime.Transient
        );

        // Must be Scoped or Transient => Cannot consume scoped service 'Microsoft.EntityFrameworkCore.DbContextOptions`
        services.AddScoped(typeof(IContentGenericRepository<>), typeof(EfCoreContentGenericRepository<>));

        return services;
    }
}