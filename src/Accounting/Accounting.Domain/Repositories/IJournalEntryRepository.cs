using Accounting.Domain.JournalEntries;

namespace Accounting.Domain.Repositories;

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNumberAsync(Guid tenantId, string entryNumber, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntry>> SearchAsync(
        Guid tenantId,
        string? fiscalPeriod,
        EntryStatus? status,
        Guid? accountId,
        string? referenceType,
        Guid? referenceId,
        CancellationToken ct = default);
    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
    Task UpdateAsync(JournalEntry entry, CancellationToken ct = default);
}
