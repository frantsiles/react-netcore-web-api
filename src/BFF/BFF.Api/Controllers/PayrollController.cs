using BFF.Application.Payroll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/payroll")]
[Authorize]
public class PayrollController(IMediator mediator) : ControllerBase
{
    [HttpPost("runs")]
    public async Task<IActionResult> Create([FromBody] CreateRunRequest body, CancellationToken ct)
        => Ok(await mediator.Send(
            new CreatePayrollRunBffCommand(GetToken(), body.PeriodType,
                body.PeriodStart, body.PeriodEnd, body.CurrencyCode), ct));

    [HttpGet("runs/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPayrollRunBffQuery(GetToken(), id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("runs")]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new ListPayrollRunsBffQuery(GetToken(), skip, take), ct));

    [HttpPost("runs/{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ConfirmPayrollRunBffCommand(GetToken(), id), ct));

    [HttpPost("runs/{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new MarkPayrollRunPaidBffCommand(GetToken(), id), ct));

    [HttpGet("runs/{runId:guid}/entries/{entryId:guid}/paystub")]
    public async Task<IActionResult> Paystub(Guid runId, Guid entryId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await mediator.Send(
            new DownloadPaystubBffQuery(GetToken(), runId, entryId), ct);
        return File(content, contentType, fileName);
    }

    [HttpGet("runs/{id:guid}/ccss-report")]
    public async Task<IActionResult> CcssReport(Guid id, CancellationToken ct)
    {
        var (content, contentType, fileName) = await mediator.Send(
            new DownloadCcssReportBffQuery(GetToken(), id), ct);
        return File(content, contentType, fileName);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreateRunRequest(
    string PeriodType,
    string PeriodStart,
    string PeriodEnd,
    string CurrencyCode);
