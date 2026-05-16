using Api.Domain.Common;

namespace Accounting.Domain.JournalEntries;

public class JournalEntryLine : Entity
{
    public Guid AccountId { get; private set; }
    public string AccountNumber { get; private set; } = "";
    public string AccountName { get; private set; } = "";
    public EntrySide Side { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }

    private JournalEntryLine() : base() { }

    public static JournalEntryLine Create(
        Guid accountId,
        string accountNumber,
        string accountName,
        EntrySide side,
        decimal amount,
        string? description = null)
    {
        if (amount <= 0)
            throw new DomainException("Journal entry line amount must be positive.");

        return new JournalEntryLine
        {
            AccountId = accountId,
            AccountNumber = accountNumber.Trim(),
            AccountName = accountName.Trim(),
            Side = side,
            Amount = Math.Round(amount, 2),
            Description = description?.Trim()
        };
    }
}
