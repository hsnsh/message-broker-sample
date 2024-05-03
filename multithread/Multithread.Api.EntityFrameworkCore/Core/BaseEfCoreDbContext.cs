using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Multithread.Api.Core;

namespace Multithread.Api.EntityFrameworkCore.Core;

public abstract class BaseEfCoreDbContext<TDbContext> : DbContext, IScopedDependency
    where TDbContext : DbContext
{
    protected BaseEfCoreDbContext(DbContextOptions<TDbContext> options)
        : base(options)
    {
        Initialize();
    }

    private void Initialize(double timeout = 30000)
    {
        if (Database.IsRelational() && !Database.GetCommandTimeout().HasValue)
        {
            Database.SetCommandTimeout(TimeSpan.FromMilliseconds(timeout));
        }

        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;

        // ChangeTracker.Tracked += ChangeTracker_Tracked;
        // ChangeTracker.StateChanged += ChangeTracker_StateChanged;
    }
}