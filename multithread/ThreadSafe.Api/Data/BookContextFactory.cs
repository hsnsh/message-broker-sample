using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ThreadSafe.Api.Data;

internal sealed class BookContextFactory : IDesignTimeDbContextFactory<BookContext>
{
    public BookContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<BookContext>()
            .UseNpgsql("Host=localhost;Port=35432;Database=BookDb;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;",
                b =>
                {
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                });

        return new BookContext(builder.Options);
    }
}