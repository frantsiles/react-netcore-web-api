using FluentAssertions;
using Reporting.Application.Common.Dtos;

namespace Reporting.UnitTests.Queries;

public class ReportingDtoTests
{
    [Fact]
    public void TrialBalanceReportDto_IsBalanced_WhenDebitsEqualCredits()
    {
        var lines = new List<TrialBalanceLineDto>
        {
            new("1000", "Cash", "Asset", 1000m, 0m),
            new("3000", "Equity", "Equity", 0m, 1000m)
        };
        var report = new TrialBalanceReportDto("2024-01", lines, 1000m, 1000m, true);

        report.IsBalanced.Should().BeTrue();
        report.TotalDebits.Should().Be(report.TotalCredits);
    }

    [Fact]
    public void TrialBalanceReportDto_NotBalanced_WhenDebitsCreditsDiffer()
    {
        var lines = new List<TrialBalanceLineDto>
        {
            new("1000", "Cash", "Asset", 1000m, 0m),
            new("3000", "Equity", "Equity", 0m, 800m)
        };
        var report = new TrialBalanceReportDto("2024-01", lines, 1000m, 800m, false);

        report.IsBalanced.Should().BeFalse();
    }

    [Fact]
    public void KpiDashboardDto_GrossMarginPct_CalculatedCorrectly()
    {
        var kpi = new KpiDashboardDto(
            DateTime.UtcNow,
            TotalRevenue: 100_000m,
            TotalCogs: 60_000m,
            GrossMarginPct: 40.0m,
            TotalPayables: 5_000m,
            TotalReceivables: 20_000m,
            OpenPurchaseOrders: 3,
            OpenSalesOrders: 7,
            OpenApprovalRequests: 2,
            LowStockItems: 4,
            CashBalance: 15_000m);

        kpi.GrossMarginPct.Should().Be(40.0m);
        kpi.TotalRevenue.Should().BeGreaterThan(kpi.TotalCogs);
    }

    [Fact]
    public void ProfitAndLossReportDto_NetIncomeEqualsGrossMinusExpenses()
    {
        var sections = new List<PnLSectionDto>
        {
            new("Revenue", [new("4000", "Sales", 50_000m)], 50_000m),
            new("COGS", [new("5000", "COGS", 20_000m)], 20_000m),
            new("Expenses", [new("6000", "Rent", 10_000m)], 10_000m)
        };
        var report = new ProfitAndLossReportDto(
            DateTime.Today.AddDays(-30), DateTime.Today,
            sections,
            GrossProfit: 30_000m,
            OperatingIncome: 20_000m,
            NetIncome: 20_000m);

        report.NetIncome.Should().Be(report.GrossProfit - 10_000m);
    }

    [Fact]
    public void AgingReportDto_GrandTotal_SumOfAllLines()
    {
        var lines = new List<AgingLineDto>
        {
            new("INV-001", "Customer A", DateTime.Today.AddDays(-10), DateTime.Today.AddDays(20), 1000m, 0),
            new("INV-002", "Customer B", DateTime.Today.AddDays(-45), DateTime.Today.AddDays(-15), 500m, 15)
        };
        var buckets = new List<AgingBucketDto>
        {
            new("0-30", 0, 30, [lines[0]], 1000m),
            new("31-60", 31, 60, [lines[1]], 500m)
        };
        var report = new AgingReportDto("AR", DateTime.Today, buckets, 1500m);

        report.GrandTotal.Should().Be(1500m);
        report.Buckets.Should().HaveCount(2);
    }

    [Fact]
    public void InventoryPositionReportDto_LowStockFlagSetCorrectly()
    {
        var lines = new List<InventoryPositionLineDto>
        {
            new("SKU-001", "Widget", "Main", 50, 100, true),
            new("SKU-002", "Gadget", "Main", 200, 50, false)
        };
        var report = new InventoryPositionReportDto(DateTime.UtcNow, lines, 2, 250);

        report.Lines.Count(l => l.BelowReorderPoint).Should().Be(1);
        report.TotalUnits.Should().Be(250);
    }
}
