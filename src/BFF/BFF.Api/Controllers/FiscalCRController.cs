using BFF.Application.FiscalCR;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/fiscal/cr/invoices")]
[Authorize]
public class FiscalCRController(IMediator mediator) : ControllerBase
{
    [HttpPost("{invoiceId:guid}/timbre")]
    public async Task<IActionResult> Timbre(Guid invoiceId, CancellationToken ct)
        => Ok(await mediator.Send(new TimbrarFacturaBffCommand(GetToken(), invoiceId), ct));

    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> GetStatus(Guid invoiceId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetElectronicDocumentBffQuery(GetToken(), invoiceId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{invoiceId:guid}/poll")]
    public async Task<IActionResult> Poll(Guid invoiceId, CancellationToken ct)
        => Ok(await mediator.Send(new PollDocumentStatusBffCommand(GetToken(), invoiceId), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}
