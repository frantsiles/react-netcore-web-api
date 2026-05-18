using FiscalCR.Domain.ElectronicDocuments;

namespace FiscalCR.Domain.Services;

public interface IElectronicDocumentRepository
{
    Task<ElectronicDocument?> FindByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task<ElectronicDocument?> FindByClaveAsync(string clave, CancellationToken ct = default);
    Task<List<ElectronicDocument>> FindPendingSubmissionsAsync(CancellationToken ct = default);
    Task AddAsync(ElectronicDocument doc, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
