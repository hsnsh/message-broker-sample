using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Multithread.Api.EntityFrameworkCore.Migrations;

public class SampleDbContextFactory : IDesignTimeDbContextFactory<SampleEfCoreDbContext>
{
    public SampleEfCoreDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SampleEfCoreDbContext>()
            .UseNpgsql("Host=localhost;Port=35432;Database=SampleDb;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;",
                b =>
                {
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                    b.MigrationsAssembly(typeof(SampleEfCoreDbContext).Assembly.GetName().Name);
                });

        return new SampleEfCoreDbContext(null, builder.Options);
    }
}