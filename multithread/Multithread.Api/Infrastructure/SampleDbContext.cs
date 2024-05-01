using Microsoft.EntityFrameworkCore;
using Multithread.Api.Infrastructure.Domain;

namespace Multithread.Api.Infrastructure;

public sealed class SampleDbContext : DbContext
{
    public DbSet<SampleEntity> Samples { get; set; }

    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SampleEntity>(b =>
        {
            b.ToTable("Samples");
            b.HasKey(ci => ci.Id);

            b.Property(x => x.Name).HasColumnName(nameof(SampleEntity.Name)).IsRequired().HasMaxLength(100);
        });
    }
}