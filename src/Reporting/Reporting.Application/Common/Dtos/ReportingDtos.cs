namespace Reporting.Application.Common.Dtos;

// ── Trial Balance ──────────────────────────────────────────────────────────────

public record TrialBalanceReportDto(
    string FiscalPeriod,
    List<TrialBalanceLineDto> Lines,
    decimal TotalDebits,
    decimal TotalCredits,
    bool IsBalanced);

public record TrialBalanceLineDto(
    string AccountNumber,
    string AccountName,
    string AccountType,
    decimal DebitBalance,
    decimal CreditBalance);

// ── Profit & Loss ──────────────────────────────────────────────────────────────

public record ProfitAndLossReportDto(
    DateTime From,
    DateTime To,
    List<PnLSectionDto> Sections,
    decimal GrossProfit,
    decimal OperatingIncome,
    decimal NetIncome);

public record PnLSectionDto(
    string Section,
    List<PnLLineDto> Lines,
    decimal Total);

public record PnLLineDto(
    string AccountNumber,
    string AccountName,
    decimal Amount);

// ── Inventory Position ─────────────────────────────────────────────────────────

public record InventoryPositionReportDto(
    DateTime AsOf,
    List<InventoryPositionLineDto> Lines,
    int TotalItems,
    int TotalUnits);

public record InventoryPositionLineDto(
    string Sku,
    string ItemName,
    string WarehouseName,
    int QuantityOnHand,
    int ReorderPoint,
    bool BelowReorderPoint);

// ── Accounts Payable / Receivable ─────────────────────────────────────────────

public record AgingReportDto(
    string Type,
    DateTime AsOf,
    List<AgingBucketDto> Buckets,
    decimal GrandTotal);

public record AgingBucketDto(
    string Label,
    int DaysFrom,
    int DaysTo,
    List<AgingLineDto> Lines,
    decimal BucketTotal);

public record AgingLineDto(
    string DocumentReference,
    string Party,
    DateTime DocumentDate,
    DateTime DueDate,
    decimal Amount,
    int DaysOverdue);

// ── KPI Dashboard ──────────────────────────────────────────────────────────────

public record KpiDashboardDto(
    DateTime AsOf,
    decimal TotalRevenue,
    decimal TotalCogs,
    decimal GrossMarginPct,
    decimal TotalPayables,
    decimal TotalReceivables,
    int OpenPurchaseOrders,
    int OpenSalesOrders,
    int OpenApprovalRequests,
    int LowStockItems,
    decimal CashBalance);
