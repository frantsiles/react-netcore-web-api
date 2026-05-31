using BFF.Application.Reporting;
using BFF.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/reports")]
[Authorize]
public class ReportingController(IMediator mediator, IApiClient apiClient) : ControllerBase
{
    [HttpGet("kpi-dashboard")]
    public async Task<IActionResult> KpiDashboard(CancellationToken ct)
        => Ok(await mediator.Send(new GetKpiDashboardBffQuery(GetToken()), ct));

    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] string fiscalPeriod, CancellationToken ct)
        => Ok(await mediator.Send(new GetTrialBalanceBffQuery(GetToken(), fiscalPeriod), ct));

    [HttpGet("profit-and-loss")]
    public async Task<IActionResult> ProfitAndLoss(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await mediator.Send(new GetProfitAndLossBffQuery(GetToken(), from, to), ct));

    [HttpGet("revenue-time-series")]
    public async Task<IActionResult> RevenueTimeSeries(
        [FromQuery] int months = 6, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetRevenueTimeSeriesBffQuery(GetToken(), months), ct));

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet(
        [FromQuery] DateTime? asOf, CancellationToken ct)
        => Ok(await mediator.Send(new GetBalanceSheetBffQuery(GetToken(), asOf), ct));

    [HttpGet("profit-and-loss/pdf")]
    public async Task<IActionResult> ProfitAndLossPdf(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var url = $"api/reports/profit-and-loss/pdf?from={from:O}&to={to:O}";
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(url, GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("balance-sheet/pdf")]
    public async Task<IActionResult> BalanceSheetPdf(
        [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var url = asOf.HasValue
            ? $"api/reports/balance-sheet/pdf?asOf={asOf.Value:O}"
            : "api/reports/balance-sheet/pdf";
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(url, GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("trial-balance/pdf")]
    public async Task<IActionResult> TrialBalancePdf(
        [FromQuery] string fiscalPeriod, CancellationToken ct)
    {
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(
            $"api/reports/trial-balance/pdf?fiscalPeriod={Uri.EscapeDataString(fiscalPeriod)}", GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("inventory-position/export")]
    public async Task<IActionResult> ExportInventory(CancellationToken ct)
    {
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(
            "api/reports/inventory-position/export", GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("accounts-receivable-aging/export")]
    public async Task<IActionResult> ExportArAging([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var url = asOf.HasValue
            ? $"api/reports/accounts-receivable-aging/export?asOf={asOf.Value:O}"
            : "api/reports/accounts-receivable-aging/export";
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(url, GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("accounts-payable-aging/export")]
    public async Task<IActionResult> ExportApAging([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var url = asOf.HasValue
            ? $"api/reports/accounts-payable-aging/export?asOf={asOf.Value:O}"
            : "api/reports/accounts-payable-aging/export";
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(url, GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}
