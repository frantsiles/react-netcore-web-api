using FiscalCR.Domain.ElectronicDocuments;
using FiscalCR.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FiscalCR.Infrastructure.Persistence;

public class ElectronicDocumentRepository(FiscalCrDbContext db) : IElectronicDocumentRepository
{
    public Task<ElectronicDocument?> FindByInvoiceIdAsync(Guid invoiceId, CancellationToken ct) =>
        db.ElectronicDocuments.FirstOrDefaultAsync(d => d.InvoiceId == invoiceId, ct);

    public Task<ElectronicDocument?> FindByClaveAsync(string clave, CancellationToken ct) =>
        db.ElectronicDocuments.FirstOrDefaultAsync(d => d.Clave == clave, ct);

    public Task<List<ElectronicDocument>> FindPendingSubmissionsAsync(CancellationToken ct) =>
        db.ElectronicDocuments
            .Where(d => d.Status == ElectronicDocumentStatus.Submitted)
            .ToListAsync(ct);

    public Task AddAsync(ElectronicDocument doc, CancellationToken ct) =>
        db.ElectronicDocuments.AddAsync(doc, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
