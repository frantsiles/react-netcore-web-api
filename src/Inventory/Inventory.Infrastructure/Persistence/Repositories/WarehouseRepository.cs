using Inventory.Domain.Repositories;
using Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly InventoryDbContext _db;

    public WarehouseRepository(InventoryDbContext db) => _db = db;

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<Warehouse?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await _db.Warehouses.FirstOrDefaultAsync(
            w => w.Code == code.ToUpperInvariant(), ct);

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default) =>
        await _db.Warehouses.AnyAsync(w => w.Code == code.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Warehouse>> ListAllAsync(CancellationToken ct = default) =>
        await _db.Warehouses.OrderBy(w => w.Code).ToListAsync(ct);

    public async Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        await _db.Warehouses.AddAsync(warehouse, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        _db.Warehouses.Update(warehouse);
        await _db.SaveChangesAsync(ct);
    }
}
