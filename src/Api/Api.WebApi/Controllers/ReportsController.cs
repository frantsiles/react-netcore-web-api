using Api.WebApi.Infrastructure.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Queries.AccountsPayableReceivable;
using Reporting.Application.Queries.BalanceSheet;
using Reporting.Application.Queries.InventoryPosition;
using Reporting.Application.Queries.KpiDashboard;
using Reporting.Application.Queries.ProfitAndLoss;
using Reporting.Application.Queries.RevenueTimeSeries;
using Reporting.Application.Queries.TrialBalance;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] string fiscalPeriod, CancellationToken ct)
        => Ok(await sender.Send(new TrialBalanceQuery(fiscalPeriod), ct));

    [HttpGet("profit-and-loss")]
    public async Task<IActionResult> ProfitAndLoss(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await sender.Send(new ProfitAndLossQuery(from, to), ct));

    [HttpGet("inventory-position")]
    public async Task<IActionResult> InventoryPosition(CancellationToken ct)
        => Ok(await sender.Send(new InventoryPositionQuery(), ct));

    [HttpGet("accounts-receivable-aging")]
    public async Task<IActionResult> AccountsReceivableAging(
        [FromQuery] DateTime? asOf, CancellationToken ct)
        => Ok(await sender.Send(new AccountsReceivableAgingQuery(asOf ?? DateTime.UtcNow), ct));

    [HttpGet("accounts-payable-aging")]
    public async Task<IActionResult> AccountsPayableAging(
        [FromQuery] DateTime? asOf, CancellationToken ct)
        => Ok(await sender.Send(new AccountsPayableAgingQuery(asOf ?? DateTime.UtcNow), ct));

    [HttpGet("kpi-dashboard")]
    public async Task<IActionResult> KpiDashboard(CancellationToken ct)
        => Ok(await sender.Send(new KpiDashboardQuery(), ct));

    [HttpGet("revenue-time-series")]
    public async Task<IActionResult> RevenueTimeSeries(
        [FromQuery] int months = 6, CancellationToken ct = default)
        => Ok(await sender.Send(new RevenueTimeSeriesQuery(months), ct));

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet(
        [FromQuery] DateTime? asOf, CancellationToken ct)
        => Ok(await sender.Send(new BalanceSheetQuery(asOf ?? DateTime.UtcNow), ct));

    [HttpGet("inventory-position/export")]
    public async Task<IActionResult> ExportInventory(CancellationToken ct)
    {
        var report = await sender.Send(new InventoryPositionQuery(), ct);
        var bytes = ExcelService.GenerateInventoryExcel(report);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Inventario-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet("accounts-receivable-aging/export")]
    public async Task<IActionResult> ExportArAging([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var report = await sender.Send(new AccountsReceivableAgingQuery(asOf ?? DateTime.UtcNow), ct);
        var bytes = ExcelService.GenerateAgingExcel(report);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"CxC-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet("accounts-payable-aging/export")]
    public async Task<IActionResult> ExportApAging([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var report = await sender.Send(new AccountsPayableAgingQuery(asOf ?? DateTime.UtcNow), ct);
        var bytes = ExcelService.GenerateAgingExcel(report);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"CxP-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
