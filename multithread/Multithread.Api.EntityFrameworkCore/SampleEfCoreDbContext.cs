using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Multithread.Api.Domain;

namespace Multithread.Api.EntityFrameworkCore;

public sealed class SampleEfCoreDbContext : BaseEfCoreDbContext<SampleEfCoreDbContext>
{
    public DbSet<SampleEntity> Samples { get; set; }

    public SampleEfCoreDbContext(IServiceProvider provider, DbContextOptions<SampleEfCoreDbContext> options) : base(options, provider)
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