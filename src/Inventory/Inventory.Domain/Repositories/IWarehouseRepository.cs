using Inventory.Domain.Warehouses;

namespace Inventory.Domain.Repositories;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Warehouse>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(Warehouse warehouse, CancellationToken ct = default);
    Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default);
}
