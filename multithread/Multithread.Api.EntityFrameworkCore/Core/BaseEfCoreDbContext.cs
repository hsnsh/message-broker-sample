using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Multithread.Api.Auditing;
using Multithread.Api.Core;
using Multithread.Api.Domain.Core.Entities;

namespace Multithread.Api.EntityFrameworkCore.Core;

public abstract class BaseEfCoreDbContext<TDbContext> : DbContext, IScopedDependency
    where TDbContext : DbContext
{
    private IServiceProvider ServiceProvider { get; set; }

    public IAuditPropertySetter AuditPropertySetter => ServiceProvider?.GetRequiredService<IAuditPropertySetter>();

    protected BaseEfCoreDbContext(IServiceProvider provider, DbContextOptions<TDbContext> options)
        : base(options)
    {
        ServiceProvider = provider;
        Initialize();
    }

    private void Initialize(double timeout = 30000)
    {
        if (Database.IsRelational() && !Database.GetCommandTimeout().HasValue)
        {
            Database.SetCommandTimeout(TimeSpan.FromMilliseconds(timeout));
        }

        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;

        ChangeTracker.Tracked += ChangeTracker_Tracked;
        ChangeTracker.StateChanged += ChangeTracker_StateChanged;
    }

    protected virtual void ChangeTracker_Tracked(object sender, EntityTrackedEventArgs e)
    {
        ApplyBaseConceptsForTrackedEntity(e.Entry);
    }

    protected virtual void ChangeTracker_StateChanged(object sender, EntityStateChangedEventArgs e)
    {
        ApplyBaseConceptsForTrackedEntity(e.Entry);
    }

    private void ApplyBaseConceptsForTrackedEntity(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                ApplyBaseConceptsForAddedEntity(entry);
                break;
            case EntityState.Modified:
                ApplyBaseConceptsForModifiedEntity(entry);
                break;
            case EntityState.Deleted:
                ApplyBaseConceptsForDeletedEntity(entry);
                break;
        }
    }

    protected virtual void ApplyBaseConceptsForAddedEntity(EntityEntry entry)
    {
        CheckAndSetId(entry);
        AuditPropertySetter?.SetCreationProperties(entry.Entity);
    }

    protected virtual void ApplyBaseConceptsForModifiedEntity(EntityEntry entry)
    {
        if (entry.State == EntityState.Modified && entry.Properties.Any(x => x.IsModified && x.Metadata.ValueGenerated == ValueGenerated.Never))
        {
            AuditPropertySetter?.SetModificationProperties(entry.Entity);
            if (entry.Entity is ISoftDelete && ((ISoftDelete)entry.Entity).IsDeleted)
            {
                AuditPropertySetter?.SetDeletionProperties(entry.Entity);
            }
        }
    }

    protected virtual void ApplyBaseConceptsForDeletedEntity(EntityEntry entry)
    {
        if (!(entry.Entity is ISoftDelete))
        {
            return;
        }

        entry.Reload();
        ((ISoftDelete)entry.Entity).IsDeleted = true;
        AuditPropertySetter?.SetDeletionProperties(entry.Entity);
    }

    protected virtual void CheckAndSetId(EntityEntry entry)
    {
        if (entry.Entity is IEntity<Guid> entityWithGuidId)
        {
            if (entityWithGuidId.Id != default)
            {
                return;
            }

            entityWithGuidId.Id = Guid.NewGuid();
        }
    }
}