using Microsoft.EntityFrameworkCore;
using Sales.Domain.Orders;
using Sales.Domain.Repositories;

namespace Sales.Infrastructure.Persistence.Repositories;

public class SalesOrderRepository : ISalesOrderRepository
{
    private readonly SalesDbContext _db;

    public SalesOrderRepository(SalesDbContext db) => _db = db;

    public async Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.SalesOrders
            .Include("_lines")
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<bool> ExistsByNumberAsync(string orderNumber, CancellationToken ct = default) =>
        await _db.SalesOrders.AnyAsync(o => o.OrderNumber == orderNumber.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<SalesOrder>> SearchAsync(Guid? customerId,
        SalesOrderStatus? status, Guid? originQuoteId, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.SalesOrders.Include("_lines").AsQueryable();

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
        if (originQuoteId.HasValue)
            query = query.Where(o => o.OriginQuoteId == originQuoteId.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(SalesOrder order, CancellationToken ct = default)
    {
        await _db.SalesOrders.AddAsync(order, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SalesOrder order, CancellationToken ct = default)
    {
        _db.SalesOrders.Update(order);
        await _db.SaveChangesAsync(ct);
    }
}
