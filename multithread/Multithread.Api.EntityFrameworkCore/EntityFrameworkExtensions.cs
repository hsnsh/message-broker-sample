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
                options.UseNpgsql(configuration.GetConnectionString("SampleDb"), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(SampleEfCoreDbContext).Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(6), null);
                    sqlOptions.CommandTimeout(30000);
                    sqlOptions.MaxBatchSize(100);
                });
                options.EnableSensitiveDataLogging(false);
                // options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
                // options.EnableSensitiveDataLogging(true);
            }
            , contextLifetime: ServiceLifetime.Scoped   // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
            , optionsLifetime: ServiceLifetime.Singleton
        );

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IContentGenericRepository<>), typeof(EfCoreContentGenericRepository<>));

        return services;
    }
}