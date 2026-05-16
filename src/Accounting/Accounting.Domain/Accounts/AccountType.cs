namespace Accounting.Domain.Accounts;

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
    ContraAsset,
    ContraRevenue
}

public static class AccountTypeExtensions
{
    /// <summary>
    /// Assets and Expenses increase with debits; Liabilities, Equity and Revenue with credits.
    /// </summary>
    public static bool NormalBalanceIsDebit(this AccountType type) =>
        type is AccountType.Asset or AccountType.ContraRevenue or AccountType.Expense;
}
