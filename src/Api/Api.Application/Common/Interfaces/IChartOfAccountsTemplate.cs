namespace Api.Application.Common.Interfaces;

public interface IChartOfAccountsTemplate
{
    string CountryCode { get; }

    /// <summary>
    /// Returns the default account plan entries for the given country.
    /// Used during tenant provisioning (E3.5) to seed the chart of accounts.
    /// </summary>
    IReadOnlyList<AccountTemplateEntry> GetEntries();
}

public sealed record AccountTemplateEntry(
    string Code,
    string Name,
    string AccountType,   // Asset, Liability, Equity, Revenue, Expense
    string NormalBalance, // Debit | Credit
    string? ParentCode);
