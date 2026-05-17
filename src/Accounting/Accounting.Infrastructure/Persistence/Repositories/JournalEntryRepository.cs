using Accounting.Domain.JournalEntries;
using Accounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence.Repositories;

public class JournalEntryRepository(AccountingDbContext db) : IJournalEntryRepository
{
    public Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<bool> ExistsByNumberAsync(Guid tenantId, string entryNumber, CancellationToken ct = default) =>
        db.JournalEntries.AnyAsync(e => e.TenantId == tenantId && e.EntryNumber == entryNumber, ct);

    public async Task<IReadOnlyList<JournalEntry>> SearchAsync(
        Guid tenantId, string? fiscalPeriod, EntryStatus? status,
        Guid? accountId, string? referenceType, Guid? referenceId,
        CancellationToken ct = default)
    {
        var query = db.JournalEntries
            .Include(e => e.Lines)
            .Where(e => e.TenantId == tenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(fiscalPeriod))
            query = query.Where(e => e.FiscalPeriod == fiscalPeriod);
        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);
        if (accountId.HasValue)
            query = query.Where(e => e.Lines.Any(l => l.AccountId == accountId.Value));
        if (!string.IsNullOrWhiteSpace(referenceType))
            query = query.Where(e => e.ReferenceType == referenceType);
        if (referenceId.HasValue)
            query = query.Where(e => e.ReferenceId == referenceId.Value);

        return await query.OrderByDescending(e => e.EntryDate).ToListAsync(ct);
    }

    public async Task AddAsync(JournalEntry entry, CancellationToken ct = default)
    {
        await db.JournalEntries.AddAsync(entry, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(JournalEntry entry, CancellationToken ct = default)
    {
        db.JournalEntries.Update(entry);
        await db.SaveChangesAsync(ct);
    }
}
