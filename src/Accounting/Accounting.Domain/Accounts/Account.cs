using Accounting.Domain.DomainEvents;
using Api.Domain.Common;

namespace Accounting.Domain.Accounts;

public class Account : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string AccountNumber { get; private set; } = "";
    public string Name { get; private set; } = "";
    public AccountType Type { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    private Account() : base() { }

    public static Account Create(
        Guid tenantId,
        string accountNumber,
        string name,
        AccountType type,
        string currencyCode,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new DomainException("Account number cannot be empty.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Account name cannot be empty.");

        var account = new Account
        {
            TenantId = tenantId,
            AccountNumber = accountNumber.Trim(),
            Name = name.Trim(),
            Type = type,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Balance = 0m,
            IsActive = true,
            Description = description?.Trim()
        };
        account.RaiseDomainEvent(new AccountCreatedEvent(
            account.Id, tenantId, account.AccountNumber, account.Name, DateTimeOffset.UtcNow));
        return account;
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Account name cannot be empty.");
        Name = name.Trim();
        Description = description?.Trim();
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("Account is already inactive.");
        IsActive = false;
        SetUpdatedAt();
    }

    /// <summary>
    /// Called by JournalEntry.Post() to update running balance.
    /// A debit increases Asset/Expense accounts; a credit increases Liability/Equity/Revenue.
    /// </summary>
    public void ApplyDebit(decimal amount)
    {
        if (amount <= 0) throw new DomainException("Amount must be positive.");
        Balance += Type.NormalBalanceIsDebit() ? amount : -amount;
        SetUpdatedAt();
    }

    public void ApplyCredit(decimal amount)
    {
        if (amount <= 0) throw new DomainException("Amount must be positive.");
        Balance += Type.NormalBalanceIsDebit() ? -amount : amount;
        SetUpdatedAt();
    }
}
