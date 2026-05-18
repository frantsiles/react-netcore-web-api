using FiscalMX.Domain.CfdiDocuments;

namespace FiscalMX.Domain.Services;

public interface ICfdiDocumentRepository
{
    Task<CfdiDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CfdiDocument?> FindByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task AddAsync(CfdiDocument doc, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
