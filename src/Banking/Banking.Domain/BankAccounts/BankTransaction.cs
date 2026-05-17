using Api.Domain.Common;

namespace Banking.Domain.BankAccounts;

public class BankTransaction : Entity
{
    public DateTime TransactionDate { get; private set; }
    public string Description { get; private set; } = "";
    public decimal Amount { get; private set; }
    public BankTransactionType Type { get; private set; }
    public BankTransactionStatus Status { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public Guid? LinkedJournalEntryId { get; private set; }

    private BankTransaction() : base() { }

    public static BankTransaction Create(
        DateTime transactionDate,
        string description,
        decimal amount,
        BankTransactionType type,
        string? referenceNumber = null)
    {
        if (amount <= 0)
            throw new DomainException("Transaction amount must be positive.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Transaction description cannot be empty.");

        return new BankTransaction
        {
            TransactionDate = transactionDate.Date,
            Description = description.Trim(),
            Amount = Math.Round(amount, 2),
            Type = type,
            Status = BankTransactionStatus.Unreconciled,
            ReferenceNumber = referenceNumber?.Trim()
        };
    }

    internal void Reconcile(Guid journalEntryId)
    {
        if (Status != BankTransactionStatus.Unreconciled)
            throw new DomainException($"Transaction cannot be reconciled in status '{Status}'.");

        Status = BankTransactionStatus.Reconciled;
        LinkedJournalEntryId = journalEntryId;
        SetUpdatedAt();
    }

    internal void Void()
    {
        if (Status == BankTransactionStatus.Voided)
            throw new DomainException("Transaction is already voided.");
        if (Status == BankTransactionStatus.Reconciled)
            throw new DomainException("Cannot void a reconciled transaction. Unreconcile it first.");

        Status = BankTransactionStatus.Voided;
        SetUpdatedAt();
    }

    internal void Unreconcile()
    {
        if (Status != BankTransactionStatus.Reconciled)
            throw new DomainException("Transaction is not reconciled.");

        Status = BankTransactionStatus.Unreconciled;
        LinkedJournalEntryId = null;
        SetUpdatedAt();
    }
}
