using FiscalCR.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/fiscal/cr/invoices")]
[Authorize]
public class FiscalCRController(IMediator mediator) : ControllerBase
{
    [HttpPost("{invoiceId:guid}/timbre")]
    public async Task<IActionResult> Timbre(Guid invoiceId, CancellationToken ct)
    {
        var result = await mediator.Send(new TimbrarFacturaCommand(invoiceId), ct);
        return Ok(result);
    }

    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> GetStatus(Guid invoiceId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetElectronicDocumentQuery(invoiceId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{invoiceId:guid}/poll")]
    public async Task<IActionResult> Poll(Guid invoiceId, CancellationToken ct)
    {
        var result = await mediator.Send(new PollDocumentStatusCommand(invoiceId), ct);
        return Ok(result);
    }
}
