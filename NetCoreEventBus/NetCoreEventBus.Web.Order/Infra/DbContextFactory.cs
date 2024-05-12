using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NetCoreEventBus.Web.Order.Infra;

public class DbContextFactory : IDesignTimeDbContextFactory<OrderEfCoreDbContext>
{
    public OrderEfCoreDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<OrderEfCoreDbContext>()
            .UseNpgsql("Host=localhost;Port=35432;Database=OrderDb;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;",
                b =>
                {
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                    b.MigrationsAssembly(typeof(OrderEfCoreDbContext).Assembly.GetName().Name);
                });

        return new OrderEfCoreDbContext(null, builder.Options);
    }
}