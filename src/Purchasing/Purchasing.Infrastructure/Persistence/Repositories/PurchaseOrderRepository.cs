using Microsoft.EntityFrameworkCore;
using Purchasing.Domain.PurchaseOrders;
using Purchasing.Domain.Repositories;

namespace Purchasing.Infrastructure.Persistence.Repositories;

public class PurchaseOrderRepository(PurchasingDbContext db) : IPurchaseOrderRepository
{
    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.PurchaseOrders
            .Include("_lines")
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsByPoNumberAsync(Guid tenantId, string poNumber, CancellationToken ct = default) =>
        db.PurchaseOrders.AnyAsync(p => p.TenantId == tenantId && p.PoNumber == poNumber, ct);

    public async Task<IReadOnlyList<PurchaseOrder>> SearchAsync(
        Guid tenantId,
        Guid? supplierId,
        PurchaseOrderStatus? status,
        string? poNumberFilter,
        CancellationToken ct = default)
    {
        var query = db.PurchaseOrders
            .Include("_lines")
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(poNumberFilter))
            query = query.Where(p => p.PoNumber.Contains(poNumberFilter));

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(PurchaseOrder po, CancellationToken ct = default)
    {
        await db.PurchaseOrders.AddAsync(po, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PurchaseOrder po, CancellationToken ct = default)
    {
        db.PurchaseOrders.Update(po);
        await db.SaveChangesAsync(ct);
    }
}
