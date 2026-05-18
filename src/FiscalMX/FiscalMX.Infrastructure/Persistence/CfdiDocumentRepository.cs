using FiscalMX.Domain.CfdiDocuments;
using FiscalMX.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FiscalMX.Infrastructure.Persistence;

public class CfdiDocumentRepository(FiscalMxDbContext db) : ICfdiDocumentRepository
{
    public Task<CfdiDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.CfdiDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<CfdiDocument?> FindByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default)
        => db.CfdiDocuments.FirstOrDefaultAsync(d => d.InvoiceId == invoiceId, ct);

    public async Task AddAsync(CfdiDocument doc, CancellationToken ct = default)
        => await db.CfdiDocuments.AddAsync(doc, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
