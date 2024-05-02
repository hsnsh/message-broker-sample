using Microsoft.EntityFrameworkCore;
using Multithread.Api.Domain;
using Multithread.Api.EntityFrameworkCore.Core;

namespace Multithread.Api.EntityFrameworkCore;

public sealed class SampleEfCoreDbContext : BaseEfCoreDbContext<SampleEfCoreDbContext>
{
    public DbSet<SampleEntity> Samples { get; set; }

    public SampleEfCoreDbContext(DbContextOptions<SampleEfCoreDbContext> options) : base(options)
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