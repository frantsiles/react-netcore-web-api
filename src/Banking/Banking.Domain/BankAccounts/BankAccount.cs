using Api.Domain.Common;
using Banking.Domain.DomainEvents;

namespace Banking.Domain.BankAccounts;

public class BankAccount : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string AccountNumber { get; private set; } = "";
    public string BankName { get; private set; } = "";
    public string CurrencyCode { get; private set; } = "";
    public decimal Balance { get; private set; }
    public BankAccountStatus Status { get; private set; }
    public Guid? LinkedAccountingAccountId { get; private set; }
    public string? Iban { get; private set; }
    public string? Swift { get; private set; }

    private readonly List<BankTransaction> _transactions = [];
    public IReadOnlyList<BankTransaction> Transactions => _transactions.AsReadOnly();

    private BankAccount() : base() { }

    public static BankAccount Create(
        Guid tenantId,
        string accountNumber,
        string bankName,
        string currencyCode,
        Guid? linkedAccountingAccountId = null,
        string? iban = null,
        string? swift = null)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new DomainException("Account number cannot be empty.");
        if (string.IsNullOrWhiteSpace(bankName))
            throw new DomainException("Bank name cannot be empty.");

        var account = new BankAccount
        {
            TenantId = tenantId,
            AccountNumber = accountNumber.Trim(),
            BankName = bankName.Trim(),
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Balance = 0m,
            Status = BankAccountStatus.Active,
            LinkedAccountingAccountId = linkedAccountingAccountId,
            Iban = iban?.Trim().ToUpperInvariant(),
            Swift = swift?.Trim().ToUpperInvariant()
        };
        account.RaiseDomainEvent(new BankAccountCreatedEvent(
            account.Id, tenantId, account.AccountNumber, account.BankName, DateTimeOffset.UtcNow));
        return account;
    }

    public void Close()
    {
        if (Status == BankAccountStatus.Closed)
            throw new DomainException("Bank account is already closed.");
        if (_transactions.Any(t => t.Status == BankTransactionStatus.Unreconciled))
            throw new DomainException("Cannot close a bank account with unreconciled transactions.");

        Status = BankAccountStatus.Closed;
        SetUpdatedAt();
    }

    public BankTransaction AddTransaction(
        DateTime transactionDate,
        string description,
        decimal amount,
        BankTransactionType type,
        string? referenceNumber = null)
    {
        if (Status == BankAccountStatus.Closed)
            throw new DomainException("Cannot add transactions to a closed bank account.");

        var tx = BankTransaction.Create(transactionDate, description, amount, type, referenceNumber);
        _transactions.Add(tx);

        Balance += type == BankTransactionType.Credit ? amount : -amount;
        Balance = Math.Round(Balance, 2);
        SetUpdatedAt();

        RaiseDomainEvent(new BankTransactionAddedEvent(
            Id, TenantId, tx.Id, amount, type.ToString(), DateTimeOffset.UtcNow));
        return tx;
    }

    public void ReconcileTransaction(Guid transactionId, Guid journalEntryId)
    {
        var tx = _transactions.FirstOrDefault(t => t.Id == transactionId)
            ?? throw new DomainException($"Transaction '{transactionId}' not found.");

        tx.Reconcile(journalEntryId);
        SetUpdatedAt();

        RaiseDomainEvent(new BankTransactionReconciledEvent(
            Id, TenantId, transactionId, journalEntryId, DateTimeOffset.UtcNow));
    }

    public void VoidTransaction(Guid transactionId)
    {
        var tx = _transactions.FirstOrDefault(t => t.Id == transactionId)
            ?? throw new DomainException($"Transaction '{transactionId}' not found.");

        // Reverse the balance effect before voiding
        Balance -= tx.Type == BankTransactionType.Credit ? tx.Amount : -tx.Amount;
        Balance = Math.Round(Balance, 2);

        tx.Void();
        SetUpdatedAt();
    }

    public void UnreconcileTransaction(Guid transactionId)
    {
        var tx = _transactions.FirstOrDefault(t => t.Id == transactionId)
            ?? throw new DomainException($"Transaction '{transactionId}' not found.");

        tx.Unreconcile();
        SetUpdatedAt();
    }
}
