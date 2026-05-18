using BFF.Application.FiscalMX;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/fiscal/mx/invoices")]
[Authorize]
public class FiscalMXController(IMediator mediator) : ControllerBase
{
    [HttpPost("{invoiceId:guid}/timbrar")]
    public async Task<IActionResult> Timbrar(Guid invoiceId, CancellationToken ct)
        => Ok(await mediator.Send(new TimbrarCfdiBffCommand(GetToken(), invoiceId), ct));

    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> GetStatus(Guid invoiceId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCfdiDocumentBffQuery(GetToken(), invoiceId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{invoiceId:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(
        Guid invoiceId, [FromBody] CancelarMxRequest body, CancellationToken ct)
        => Ok(await mediator.Send(
            new CancelarCfdiBffCommand(GetToken(), invoiceId, body.Motivo, body.UuidRelacionado), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CancelarMxRequest(string Motivo, string? UuidRelacionado = null);
