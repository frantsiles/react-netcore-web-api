using ControlPlane.Domain.Repositories;
using ControlPlane.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace ControlPlane.Infrastructure.Persistence.Repositories;

public class TenantRepository(ControlPlaneDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tenants
            .Include("_settings")
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Tenants
            .Include("_settings")
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Tenants.AnyAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<Tenant>> ListAsync(
        string? nameFilter,
        TenantStatus? status,
        TenantPlan? plan,
        CancellationToken ct = default)
    {
        var query = db.Tenants.Include("_settings").AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(t => t.Name.Contains(nameFilter));
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        if (plan.HasValue)
            query = query.Where(t => t.Plan == plan.Value);

        return await query.OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        await db.Tenants.AddAsync(tenant, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Update(tenant);
        await db.SaveChangesAsync(ct);
    }
}
