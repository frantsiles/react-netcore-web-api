using Api.WebApi.Infrastructure.Pdf;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands;
using Payroll.Application.Queries;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController(IMediator mediator) : ControllerBase
{
    [HttpPost("runs")]
    public async Task<IActionResult> Create([FromBody] CreatePayrollRunCommand cmd, CancellationToken ct)
        => Ok(await mediator.Send(cmd, ct));

    [HttpGet("runs/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPayrollRunQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("runs")]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new ListPayrollRunsQuery(skip, take), ct));

    [HttpPost("runs/{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ConfirmPayrollRunCommand(id), ct));

    [HttpGet("runs/{runId:guid}/entries/{entryId:guid}/paystub")]
    public async Task<IActionResult> Paystub(Guid runId, Guid entryId, CancellationToken ct)
    {
        var run = await mediator.Send(new GetPayrollRunQuery(runId), ct);
        if (run is null) return NotFound();
        var entry = run.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null) return NotFound();
        var pdf = PdfService.GeneratePaystubPdf(run, entry, "ERP Platform");
        return File(pdf, "application/pdf", $"recibo-{run.RunNumber}-{entry.EmployeeNumber}.pdf");
    }
}
