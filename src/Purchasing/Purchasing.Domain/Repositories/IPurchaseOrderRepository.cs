using Purchasing.Domain.PurchaseOrders;

namespace Purchasing.Domain.Repositories;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByPoNumberAsync(Guid tenantId, string poNumber, CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseOrder>> SearchAsync(
        Guid tenantId,
        Guid? supplierId,
        PurchaseOrderStatus? status,
        string? poNumberFilter,
        CancellationToken ct = default);
    Task AddAsync(PurchaseOrder po, CancellationToken ct = default);
    Task UpdateAsync(PurchaseOrder po, CancellationToken ct = default);
}
