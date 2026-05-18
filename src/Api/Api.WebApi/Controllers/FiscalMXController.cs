using FiscalMX.Application.Commands;
using FiscalMX.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/fiscal/mx/invoices")]
[Authorize]
public class FiscalMXController(IMediator mediator) : ControllerBase
{
    [HttpPost("{invoiceId:guid}/timbrar")]
    public async Task<IActionResult> Timbrar(Guid invoiceId, CancellationToken ct)
        => Ok(await mediator.Send(new TimbrarCfdiCommand(invoiceId), ct));

    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> GetStatus(Guid invoiceId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCfdiDocumentQuery(invoiceId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{invoiceId:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(
        Guid invoiceId,
        [FromBody] CancelarRequest body,
        CancellationToken ct)
        => Ok(await mediator.Send(
            new CancelCfdiCommand(invoiceId, body.Motivo, body.UuidRelacionado), ct));
}

public record CancelarRequest(string Motivo, string? UuidRelacionado = null);
