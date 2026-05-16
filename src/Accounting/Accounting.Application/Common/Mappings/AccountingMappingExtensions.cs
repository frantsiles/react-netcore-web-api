using Accounting.Application.Common.Dtos;
using Accounting.Domain.Accounts;
using Accounting.Domain.JournalEntries;

namespace Accounting.Application.Common.Mappings;

public static class AccountingMappingExtensions
{
    public static AccountDto ToDto(this Account a) =>
        new(a.Id, a.TenantId, a.AccountNumber, a.Name, a.Type,
            a.CurrencyCode, a.Balance, a.IsActive, a.Description,
            a.CreatedAt, a.UpdatedAt);

    public static JournalEntryLineDto ToDto(this JournalEntryLine l) =>
        new(l.Id, l.AccountId, l.AccountNumber, l.AccountName, l.Side, l.Amount, l.Description);

    public static JournalEntryDto ToDto(this JournalEntry e) =>
        new(e.Id, e.TenantId, e.EntryNumber, e.FiscalPeriod, e.EntryDate,
            e.Status, e.Description, e.ReferenceType, e.ReferenceId,
            e.TotalDebits, e.TotalCredits, e.IsBalanced,
            e.Lines.Select(l => l.ToDto()).ToList(),
            e.CreatedAt, e.UpdatedAt);
}
