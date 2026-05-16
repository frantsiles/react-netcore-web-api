using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Application.Orders.Commands.CreateSalesOrder;
using Sales.Application.Quotes.Commands.AcceptQuote;
using Sales.Application.Quotes.Commands.AddQuoteLine;
using Sales.Application.Quotes.Commands.ConvertQuoteToOrder;
using Sales.Application.Quotes.Commands.CreateQuote;
using Sales.Application.Quotes.Commands.RejectQuote;
using Sales.Application.Quotes.Commands.RemoveQuoteLine;
using Sales.Application.Quotes.Commands.SendQuote;
using Sales.Application.Quotes.Commands.UpdateQuoteLine;
using Sales.Application.Quotes.Queries.GetQuoteById;
using Sales.Application.Quotes.Queries.SearchQuotes;
using Sales.Domain.Quotes;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/sales/quotes")]
[Authorize]
public class QuotesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuoteCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetQuoteByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? customerId,
        [FromQuery] QuoteStatus? status,
        [FromQuery] DateOnly? validFrom,
        [FromQuery] DateOnly? validTo,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new SearchQuotesQuery(customerId, status, validFrom, validTo, skip, take), ct));

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(Guid id,
        [FromBody] AddQuoteLineRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new AddQuoteLineCommand(id,
            body.CatalogItemId, body.SKU, body.ItemName, body.Quantity,
            body.UnitPrice, body.CurrencyCode, body.DiscountPercent, body.Notes), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId,
        [FromBody] UpdateQuoteLineRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateQuoteLineCommand(id, lineId,
            body.Quantity, body.UnitPrice, body.CurrencyCode, body.DiscountPercent, body.Notes), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct) =>
        Ok(await mediator.Send(new RemoveQuoteLineCommand(id, lineId), ct));

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new SendQuoteCommand(id), ct));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new AcceptQuoteCommand(id), ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new RejectQuoteCommand(id), ct));

    /// <summary>QuickBooks-style: convierte cotización aceptada en orden de venta en un click.</summary>
    [HttpPost("{id:guid}/convert-to-order")]
    public async Task<IActionResult> ConvertToOrder(Guid id,
        [FromBody] ConvertQuoteToOrderRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new ConvertQuoteToOrderCommand(id,
            body.RequestedDeliveryDate, body.Notes), ct);
        return CreatedAtAction("GetById", "SalesOrders", new { id = result.Id }, result);
    }
}

public record AddQuoteLineRequest(
    Guid CatalogItemId, string SKU, string ItemName,
    decimal Quantity, decimal UnitPrice, string CurrencyCode,
    decimal DiscountPercent = 0, string? Notes = null);

public record UpdateQuoteLineRequest(
    decimal Quantity, decimal UnitPrice, string CurrencyCode,
    decimal DiscountPercent = 0, string? Notes = null);

public record ConvertQuoteToOrderRequest(
    DateOnly? RequestedDeliveryDate = null, string? Notes = null);
