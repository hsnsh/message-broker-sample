using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetCoreEventBus.Web.Order.Infra.Domain;

namespace NetCoreEventBus.Web.Order.Infra;

public sealed class OrderEfCoreDbContext : BaseEfCoreDbContext<OrderEfCoreDbContext>
{
    public DbSet<OrderEntity> Orders { get; set; }

    public OrderEfCoreDbContext(IServiceProvider provider, DbContextOptions<OrderEfCoreDbContext> options) : base(options, provider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderEntity>(b =>
        {
            b.ToTable("Samples");
            b.HasKey(ci => ci.Id);

            b.Property(x => x.Name).HasColumnName(nameof(OrderEntity.Name)).IsRequired().HasMaxLength(100);
        });
    }
}