using BFF.Application.Reporting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/reports")]
[Authorize]
public class ReportingController(IMediator mediator) : ControllerBase
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

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}
