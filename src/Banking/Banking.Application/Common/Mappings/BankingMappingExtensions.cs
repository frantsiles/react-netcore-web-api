using Banking.Application.Common.Dtos;
using Banking.Domain.BankAccounts;

namespace Banking.Application.Common.Mappings;

public static class BankingMappingExtensions
{
    public static BankTransactionDto ToDto(this BankTransaction t) =>
        new(t.Id, t.TransactionDate, t.Description, t.Amount, t.Type,
            t.Status, t.ReferenceNumber, t.LinkedJournalEntryId, t.CreatedAt);

    public static BankAccountDto ToDto(this BankAccount a) =>
        new(a.Id, a.TenantId, a.AccountNumber, a.BankName, a.CurrencyCode,
            a.Balance, a.Status, a.LinkedAccountingAccountId, a.Iban, a.Swift,
            a.Transactions.Count,
            a.Transactions.Count(t => t.Status == BankTransactionStatus.Unreconciled),
            a.CreatedAt, a.UpdatedAt);
}
