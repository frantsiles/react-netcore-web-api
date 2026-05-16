using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

/// <summary>
/// Base DbContext that applies a global TenantId query filter to all ITenantEntity sets.
/// Parties, Catalog and future bounded-context DbContexts inherit from this.
/// AppDbContext (Identity) does NOT inherit — Identity entities are not tenant-scoped.
/// </summary>
public abstract class TenantAwareDbContext(
    DbContextOptions options,
    ITenantContext tenantContext) : DbContext(options)
{
    protected Guid CurrentTenantId => tenantContext.TenantId;

    protected void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>()
               .HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenantId();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken ct = default)
    {
        StampTenantId();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void StampTenantId()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>()
                     .Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity.TenantId == Guid.Empty)
                entry.Entity.TenantId = CurrentTenantId;
        }
    }
}
