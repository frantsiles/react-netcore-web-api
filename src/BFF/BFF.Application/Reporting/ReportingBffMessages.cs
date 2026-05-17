namespace BFF.Application.Reporting;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record KpiDashboardBffDto(
    DateTime AsOf, decimal TotalRevenue, decimal TotalCogs, decimal GrossMarginPct,
    decimal TotalPayables, decimal TotalReceivables, int OpenPurchaseOrders,
    int OpenSalesOrders, int OpenApprovalRequests, int LowStockItems, decimal CashBalance);

public record TrialBalanceLineBffDto(
    string AccountNumber, string AccountName, string AccountType,
    decimal DebitBalance, decimal CreditBalance);

public record TrialBalanceBffDto(
    string FiscalPeriod, List<TrialBalanceLineBffDto> Lines,
    decimal TotalDebits, decimal TotalCredits, bool IsBalanced);

public record PnLLineBffDto(string AccountNumber, string AccountName, decimal Amount);
public record PnLSectionBffDto(string Section, List<PnLLineBffDto> Lines, decimal Total);
public record ProfitAndLossBffDto(
    DateTime From, DateTime To, List<PnLSectionBffDto> Sections,
    decimal GrossProfit, decimal OperatingIncome, decimal NetIncome);

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetKpiDashboardBffQuery(string Token)
    : MediatR.IRequest<KpiDashboardBffDto>;

public record GetTrialBalanceBffQuery(string Token, string FiscalPeriod)
    : MediatR.IRequest<TrialBalanceBffDto>;

public record GetProfitAndLossBffQuery(string Token, DateTime From, DateTime To)
    : MediatR.IRequest<ProfitAndLossBffDto>;
