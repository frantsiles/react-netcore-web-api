using Invoicing.Domain.Invoices;
using Invoicing.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly InvoicingDbContext _db;

    public InvoiceRepository(InvoicingDbContext db) => _db = db;

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<bool> ExistsByNumberAsync(string invoiceNumber, CancellationToken ct = default) =>
        await _db.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Invoice>> SearchAsync(
        Guid? customerId, InvoiceStatus? status, Guid? originSalesOrderId,
        int skip, int take, CancellationToken ct = default)
    {
        var query = _db.Invoices.Include(i => i.Lines).AsQueryable();

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        if (originSalesOrderId.HasValue)
            query = query.Where(i => i.OriginSalesOrderId == originSalesOrderId.Value);

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        await _db.Invoices.AddAsync(invoice, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        _db.Invoices.Update(invoice);
        await _db.SaveChangesAsync(ct);
    }
}
