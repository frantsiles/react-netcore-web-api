using BFF.Application.Invoicing;
using BFF.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/invoicing/invoices")]
[Authorize]
public class InvoicingController(IMediator mediator, IApiClient apiClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] Guid? originSalesOrderId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new SearchInvoicesBffQuery(GetToken(), customerId, status, originSalesOrderId, skip, take), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceByIdBffQuery(GetToken(), id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("convert")]
    public async Task<IActionResult> ConvertFromOrder(
        [FromBody] ConvertFromOrderBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ConvertOrderToInvoiceBffCommand(GetToken(), req.SalesOrderId, req.DueDate, req.Notes), ct);
        return Created(string.Empty, result);
    }

    [HttpPost("{id:guid}/issue")]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new IssueInvoiceBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(
        Guid id, [FromBody] RecordPaymentBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(
            new RecordInvoicePaymentBffCommand(GetToken(), id, req.Amount, req.CurrencyCode, req.PaidAt, req.Reference), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelInvoiceBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new CancelInvoiceBffCommand(GetToken(), id, req.Reason), ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(
            $"api/invoicing/invoices/{id}/pdf", GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record ConvertFromOrderBffRequest(Guid SalesOrderId, string DueDate, string? Notes);
public record RecordPaymentBffRequest(decimal Amount, string CurrencyCode, string PaidAt, string? Reference);
public record CancelInvoiceBffRequest(string Reason);
