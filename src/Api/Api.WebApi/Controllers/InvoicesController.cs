using Invoicing.Application.Commands;
using Invoicing.Application.Queries;
using Invoicing.Domain.Invoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/invoicing/invoices")]
[Authorize]
public class InvoicesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] Guid? originSalesOrderId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        InvoiceStatus? statusEnum = status is null ? null
            : Enum.TryParse<InvoiceStatus>(status, true, out var s) ? s : null;

        var result = await mediator.Send(
            new SearchInvoicesQuery(customerId, statusEnum, originSalesOrderId, skip, take), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("convert")]
    public async Task<IActionResult> ConvertFromOrder(
        [FromBody] ConvertFromOrderRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ConvertSalesOrderToInvoiceCommand(req.SalesOrderId, req.DueDate, req.Notes), ct);
        return Created($"api/invoicing/invoices/{result.Id}", result);
    }

    [HttpPost("{id:guid}/issue")]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new IssueInvoiceCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(
        Guid id, [FromBody] RecordPaymentRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new RecordPaymentCommand(id, req.Amount, req.CurrencyCode, req.PaidAt, req.Reference), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelInvoiceRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelInvoiceCommand(id, req.Reason), ct);
        return Ok(result);
    }
}

public record ConvertFromOrderRequest(Guid SalesOrderId, DateOnly DueDate, string? Notes);
public record RecordPaymentRequest(decimal Amount, string CurrencyCode, DateOnly PaidAt, string? Reference);
public record CancelInvoiceRequest(string Reason);
