using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Multithread.Api.Infrastructure;

namespace Multithread.Api.Migrations;

internal sealed class SampleDbContextFactory : IDesignTimeDbContextFactory<SampleDbContext>
{
    public SampleDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>()
            .UseNpgsql("Host=localhost;Port=35432;Database=SampleDb;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;",
                b =>
                {
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                });

        return new SampleDbContext(builder.Options);
    }
}