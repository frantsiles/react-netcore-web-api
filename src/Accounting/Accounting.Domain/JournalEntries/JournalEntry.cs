using Accounting.Domain.DomainEvents;
using Api.Domain.Common;

namespace Accounting.Domain.JournalEntries;

public class JournalEntry : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string EntryNumber { get; private set; } = "";
    public string FiscalPeriod { get; private set; } = "";  // "YYYY-MM"
    public DateTime EntryDate { get; private set; }
    public EntryStatus Status { get; private set; }
    public string Description { get; private set; } = "";
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    private readonly List<JournalEntryLine> _lines = [];
    public IReadOnlyList<JournalEntryLine> Lines => _lines.AsReadOnly();

    public decimal TotalDebits => _lines.Where(l => l.Side == EntrySide.Debit).Sum(l => l.Amount);
    public decimal TotalCredits => _lines.Where(l => l.Side == EntrySide.Credit).Sum(l => l.Amount);
    public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.01m;

    private JournalEntry() : base() { }

    public static JournalEntry Create(
        Guid tenantId,
        string entryNumber,
        DateTime entryDate,
        string description,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        if (string.IsNullOrWhiteSpace(entryNumber))
            throw new DomainException("Entry number cannot be empty.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Journal entry description cannot be empty.");

        return new JournalEntry
        {
            TenantId = tenantId,
            EntryNumber = entryNumber.Trim(),
            FiscalPeriod = entryDate.ToString("yyyy-MM"),
            EntryDate = entryDate.Date,
            Status = EntryStatus.Draft,
            Description = description.Trim(),
            ReferenceType = referenceType?.Trim(),
            ReferenceId = referenceId
        };
    }

    private void GuardDraft()
    {
        if (Status != EntryStatus.Draft)
            throw new DomainException($"Journal entry cannot be modified in status '{Status}'.");
    }

    public void AddLine(Guid accountId, string accountNumber, string accountName,
        EntrySide side, decimal amount, string? description = null)
    {
        GuardDraft();
        _lines.Add(JournalEntryLine.Create(accountId, accountNumber, accountName,
            side, amount, description));
        SetUpdatedAt();
    }

    public void RemoveLine(Guid lineId)
    {
        GuardDraft();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Line '{lineId}' not found.");
        _lines.Remove(line);
        SetUpdatedAt();
    }

    /// <summary>
    /// Posts the entry. Requires at least 2 lines and a balanced entry (Σdebits = Σcredits).
    /// Returns the list of lines so the handler can update account balances.
    /// </summary>
    public IReadOnlyList<JournalEntryLine> Post()
    {
        GuardDraft();
        if (_lines.Count < 2)
            throw new DomainException("A journal entry requires at least 2 lines.");
        if (!IsBalanced)
            throw new DomainException(
                $"Journal entry is not balanced: debits={TotalDebits:F2}, credits={TotalCredits:F2}.");

        Status = EntryStatus.Posted;
        SetUpdatedAt();
        RaiseDomainEvent(new JournalEntryPostedEvent(
            Id, TenantId, EntryNumber, TotalDebits, FiscalPeriod, DateTimeOffset.UtcNow));

        return _lines.AsReadOnly();
    }

    /// <summary>
    /// Creates a reversing entry (mirror of this one) in the given period.
    /// </summary>
    public JournalEntry CreateReversal(string reversalEntryNumber, DateTime reversalDate)
    {
        if (Status != EntryStatus.Posted)
            throw new DomainException("Only Posted entries can be reversed.");

        var reversal = new JournalEntry
        {
            TenantId = TenantId,
            EntryNumber = reversalEntryNumber.Trim(),
            FiscalPeriod = reversalDate.ToString("yyyy-MM"),
            EntryDate = reversalDate.Date,
            Status = EntryStatus.Draft,
            Description = $"Reversal of {EntryNumber}: {Description}",
            ReferenceType = "JournalEntry",
            ReferenceId = Id
        };

        foreach (var line in _lines)
        {
            var flippedSide = line.Side == EntrySide.Debit ? EntrySide.Credit : EntrySide.Debit;
            reversal._lines.Add(JournalEntryLine.Create(
                line.AccountId, line.AccountNumber, line.AccountName,
                flippedSide, line.Amount, $"Reversal: {line.Description}"));
        }

        Status = EntryStatus.Reversed;
        SetUpdatedAt();
        RaiseDomainEvent(new JournalEntryReversedEvent(
            Id, TenantId, EntryNumber, reversal.EntryNumber, DateTimeOffset.UtcNow));

        return reversal;
    }
}
