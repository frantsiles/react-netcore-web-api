using Accounting.Domain.Accounts;
using Accounting.Infrastructure.Persistence;
using Approvals.Domain.ApprovalRequests;
using Approvals.Infrastructure.Persistence;
using Banking.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Purchasing.Domain.PurchaseOrders;
using Purchasing.Infrastructure.Persistence;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;
using Sales.Domain.Orders;
using Sales.Infrastructure.Persistence;

namespace Reporting.Infrastructure;

public class ReportingStore(
    AccountingDbContext accountingDb,
    InventoryDbContext inventoryDb,
    PurchasingDbContext purchasingDb,
    SalesDbContext salesDb,
    BankingDbContext bankingDb,
    ApprovalsDbContext approvalsDb)
    : IReportingStore
{
    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(string fiscalPeriod, CancellationToken ct = default)
    {
        var accounts = await accountingDb.Accounts.AsNoTracking().ToListAsync(ct);

        var lines = accounts.Select(a =>
        {
            var isDebitNormal = a.Type.NormalBalanceIsDebit();
            return new TrialBalanceLineDto(
                a.AccountNumber, a.Name, a.Type.ToString(),
                DebitBalance: isDebitNormal ? Math.Max(a.Balance, 0) : Math.Max(-a.Balance, 0),
                CreditBalance: isDebitNormal ? Math.Max(-a.Balance, 0) : Math.Max(a.Balance, 0));
        }).ToList();

        var totalDebits = lines.Sum(l => l.DebitBalance);
        var totalCredits = lines.Sum(l => l.CreditBalance);

        return new TrialBalanceReportDto(fiscalPeriod, lines, totalDebits, totalCredits,
            Math.Abs(totalDebits - totalCredits) < 0.01m);
    }

    public async Task<ProfitAndLossReportDto> GetProfitAndLossAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var accounts = await accountingDb.Accounts.AsNoTracking()
            .Where(a => a.Type == AccountType.Revenue || a.Type == AccountType.Expense ||
                        a.Type == AccountType.ContraRevenue)
            .ToListAsync(ct);

        var revenueLines = accounts.Where(a => a.Type == AccountType.Revenue)
            .Select(a => new PnLLineDto(a.AccountNumber, a.Name, a.Balance)).ToList();
        var cogsLines = accounts.Where(a => a.Type == AccountType.ContraRevenue)
            .Select(a => new PnLLineDto(a.AccountNumber, a.Name, a.Balance)).ToList();
        var expenseLines = accounts.Where(a => a.Type == AccountType.Expense)
            .Select(a => new PnLLineDto(a.AccountNumber, a.Name, a.Balance)).ToList();

        var totalRevenue = revenueLines.Sum(l => l.Amount);
        var totalCogs = cogsLines.Sum(l => l.Amount);
        var totalExpenses = expenseLines.Sum(l => l.Amount);
        var grossProfit = totalRevenue - totalCogs;
        var netIncome = grossProfit - totalExpenses;

        var sections = new List<PnLSectionDto>
        {
            new("Revenue", revenueLines, totalRevenue),
            new("Cost of Goods Sold", cogsLines, totalCogs),
            new("Operating Expenses", expenseLines, totalExpenses)
        };

        return new ProfitAndLossReportDto(from, to, sections, grossProfit, grossProfit - totalExpenses, netIncome);
    }

    public async Task<InventoryPositionReportDto> GetInventoryPositionAsync(CancellationToken ct = default)
    {
        var items = await inventoryDb.InventoryItems.AsNoTracking().ToListAsync(ct);
        var warehouses = await inventoryDb.Warehouses.AsNoTracking().ToListAsync(ct);
        var warehouseMap = warehouses.ToDictionary(w => w.Id, w => w.Name);

        var lines = items.Select(i => new InventoryPositionLineDto(
            i.SKU, i.SKU,
            warehouseMap.GetValueOrDefault(i.WarehouseId, "Unknown"),
            (int)i.QuantityOnHand,
            i.ReorderPoint.HasValue ? (int)i.ReorderPoint.Value : 0,
            i.ReorderPoint.HasValue && i.QuantityOnHand <= i.ReorderPoint.Value)).ToList();

        return new InventoryPositionReportDto(
            DateTime.UtcNow, lines, lines.Count, lines.Sum(l => l.QuantityOnHand));
    }

    public async Task<AgingReportDto> GetAccountsReceivableAgingAsync(DateTime asOf, CancellationToken ct = default)
    {
        var orders = await salesDb.SalesOrders.AsNoTracking()
            .Where(o => o.Status == SalesOrderStatus.Confirmed || o.Status == SalesOrderStatus.PartiallyFulfilled)
            .ToListAsync(ct);

        var lines = orders.Select(o => new AgingLineDto(
            o.OrderNumber, o.CustomerId.ToString(),
            o.OrderDate.ToDateTime(TimeOnly.MinValue),
            o.OrderDate.AddDays(30).ToDateTime(TimeOnly.MinValue),
            o.Total.Amount,
            Math.Max(0, (int)(asOf - o.OrderDate.AddDays(30).ToDateTime(TimeOnly.MinValue)).TotalDays)
        )).ToList();

        return BuildAgingReport("AR", asOf, lines);
    }

    public async Task<AgingReportDto> GetAccountsPayableAgingAsync(DateTime asOf, CancellationToken ct = default)
    {
        var orders = await purchasingDb.PurchaseOrders.AsNoTracking()
            .Where(o => o.Status == PurchaseOrderStatus.Confirmed || o.Status == PurchaseOrderStatus.PartiallyReceived)
            .ToListAsync(ct);

        var lines = orders.Select(o => new AgingLineDto(
            o.PoNumber, o.SupplierId.ToString(),
            o.CreatedAt, o.ExpectedDeliveryDate ?? o.CreatedAt.AddDays(30),
            o.Total.Amount,
            Math.Max(0, (int)(asOf - (o.ExpectedDeliveryDate ?? o.CreatedAt.AddDays(30))).TotalDays)
        )).ToList();

        return BuildAgingReport("AP", asOf, lines);
    }

    public async Task<KpiDashboardDto> GetKpiDashboardAsync(CancellationToken ct = default)
    {
        var accounts = await accountingDb.Accounts.AsNoTracking().ToListAsync(ct);
        var totalRevenue = accounts.Where(a => a.Type == AccountType.Revenue).Sum(a => a.Balance);
        var totalCogs = accounts.Where(a => a.Type == AccountType.ContraRevenue).Sum(a => a.Balance);
        var grossMargin = totalRevenue > 0 ? (totalRevenue - totalCogs) / totalRevenue * 100 : 0;

        var openPOs = await purchasingDb.PurchaseOrders.AsNoTracking()
            .CountAsync(o => o.Status == PurchaseOrderStatus.Confirmed ||
                             o.Status == PurchaseOrderStatus.Sent ||
                             o.Status == PurchaseOrderStatus.PartiallyReceived, ct);

        var openSOs = await salesDb.SalesOrders.AsNoTracking()
            .CountAsync(o => o.Status == SalesOrderStatus.Confirmed ||
                             o.Status == SalesOrderStatus.PartiallyFulfilled, ct);

        var pendingApprovals = await approvalsDb.ApprovalRequests.AsNoTracking()
            .CountAsync(r => r.Status == ApprovalRequestStatus.Pending, ct);

        var inventoryItems = await inventoryDb.InventoryItems.AsNoTracking().ToListAsync(ct);
        var lowStock = inventoryItems.Count(i => i.ReorderPoint.HasValue && i.QuantityOnHand <= i.ReorderPoint.Value);

        var bankAccounts = await bankingDb.BankAccounts.AsNoTracking().ToListAsync(ct);
        var cashBalance = bankAccounts.Sum(b => b.Balance);

        var totalPayables = await purchasingDb.PurchaseOrders.AsNoTracking()
            .Where(o => o.Status == PurchaseOrderStatus.Confirmed || o.Status == PurchaseOrderStatus.PartiallyReceived)
            .SumAsync(o => (decimal?)o.Total.Amount ?? 0m, ct);

        var totalReceivables = await salesDb.SalesOrders.AsNoTracking()
            .Where(o => o.Status == SalesOrderStatus.Confirmed || o.Status == SalesOrderStatus.PartiallyFulfilled)
            .SumAsync(o => (decimal?)o.Total.Amount ?? 0m, ct);

        return new KpiDashboardDto(
            DateTime.UtcNow, totalRevenue, totalCogs, Math.Round(grossMargin, 1),
            totalPayables, totalReceivables,
            openPOs, openSOs, pendingApprovals, lowStock, cashBalance);
    }

    public async Task<RevenueTimeSeriesDto> GetRevenueTimeSeriesAsync(int months, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months + 1).Date;
        cutoff = new DateTime(cutoff.Year, cutoff.Month, 1);

        var entries = await accountingDb.JournalEntries
            .AsNoTracking()
            .Where(je => je.Status == Accounting.Domain.JournalEntries.EntryStatus.Posted && je.EntryDate >= cutoff)
            .Include(je => je.Lines)
            .ToListAsync(ct);

        var revenueAccounts = await accountingDb.Accounts.AsNoTracking()
            .Where(a => a.Type == AccountType.Revenue || a.Type == AccountType.ContraRevenue)
            .Select(a => new { a.Id, a.Type })
            .ToListAsync(ct);

        var revenueIds = revenueAccounts
            .Where(a => a.Type == AccountType.Revenue)
            .Select(a => a.Id).ToHashSet();
        var cogsIds = revenueAccounts
            .Where(a => a.Type == AccountType.ContraRevenue)
            .Select(a => a.Id).ToHashSet();

        var salesByMonth = await salesDb.SalesOrders.AsNoTracking()
            .Where(o => o.OrderDate >= DateOnly.FromDateTime(cutoff))
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var purchasesByMonth = await purchasingDb.PurchaseOrders.AsNoTracking()
            .Where(o => o.CreatedAt >= cutoff)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var revenue = new List<MonthlyRevenueDto>();
        var orders = new List<MonthlyOrdersDto>();

        for (var i = 0; i < months; i++)
        {
            var month = cutoff.AddMonths(i);
            var label = month.ToString("MMM yyyy");

            var monthEntries = entries
                .Where(je => je.EntryDate.Year == month.Year && je.EntryDate.Month == month.Month)
                .SelectMany(je => je.Lines)
                .ToList();

            var rev = monthEntries
                .Where(l => revenueIds.Contains(l.AccountId) && l.Side == Accounting.Domain.JournalEntries.EntrySide.Credit)
                .Sum(l => l.Amount);

            var cogs = monthEntries
                .Where(l => cogsIds.Contains(l.AccountId) && l.Side == Accounting.Domain.JournalEntries.EntrySide.Debit)
                .Sum(l => l.Amount);

            revenue.Add(new MonthlyRevenueDto(label, rev, cogs));

            var soCnt = salesByMonth.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Count ?? 0;
            var poCnt = purchasesByMonth.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Count ?? 0;
            orders.Add(new MonthlyOrdersDto(label, soCnt, poCnt));
        }

        return new RevenueTimeSeriesDto(revenue, orders);
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime asOf, CancellationToken ct = default)
    {
        var accounts = await accountingDb.Accounts.AsNoTracking().ToListAsync(ct);

        var assetLines = accounts
            .Where(a => a.Type == AccountType.Asset || a.Type == AccountType.ContraAsset)
            .Select(a => new PnLLineDto(a.AccountNumber, a.Name, Math.Abs(a.Balance)))
            .OrderBy(l => l.AccountNumber).ToList();

        var liabilityLines = accounts
            .Where(a => a.Type == AccountType.Liability)
            .Select(a => new PnLLineDto(a.AccountNumber, a.Name, Math.Abs(a.Balance)))
            .OrderBy(l => l.AccountNumber).ToList();

        var equityLines = accounts
            .Where(a => a.Type == AccountType.Equity)
            .Select(a => new PnLLineDto(a.AccountNumber, a.Name, Math.Abs(a.Balance)))
            .OrderBy(l => l.AccountNumber).ToList();

        var totalAssets = assetLines.Sum(l => l.Amount);
        var totalLiabilities = liabilityLines.Sum(l => l.Amount);
        var totalEquity = equityLines.Sum(l => l.Amount);

        return new BalanceSheetReportDto(
            asOf,
            new BalanceSheetSectionDto("Activos", assetLines, totalAssets),
            new BalanceSheetSectionDto("Pasivos", liabilityLines, totalLiabilities),
            new BalanceSheetSectionDto("Capital", equityLines, totalEquity),
            IsBalanced: Math.Abs(totalAssets - (totalLiabilities + totalEquity)) < 0.01m);
    }

    private static AgingReportDto BuildAgingReport(string type, DateTime asOf, List<AgingLineDto> lines)
    {
        var bucketDefs = new[] { (0, 30, "0-30"), (31, 60, "31-60"), (61, 90, "61-90"), (91, int.MaxValue, "90+") };
        var buckets = bucketDefs.Select(b =>
        {
            var bucketLines = lines.Where(l => l.DaysOverdue >= b.Item1 && l.DaysOverdue <= b.Item2).ToList();
            return new AgingBucketDto(b.Item3, b.Item1, b.Item2, bucketLines, bucketLines.Sum(l => l.Amount));
        }).ToList();

        return new AgingReportDto(type, asOf, buckets, lines.Sum(l => l.Amount));
    }
}
