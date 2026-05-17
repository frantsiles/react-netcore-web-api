using BFF.Application.Sales;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

// ── Quotes ────────────────────────────────────────────────────────────────────

[ApiController]
[Route("bff/sales/quotes")]
[Authorize]
public class SalesQuotesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new SearchQuotesBffQuery(GetToken(), customerId, status, skip, take), ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateQuoteBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateQuoteBffCommand(
            GetToken(), req.CustomerId, req.ValidUntil, req.CurrencyCode, req.CountryCode, req.Notes), ct);
        return Created(string.Empty, result);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(
        Guid id, [FromBody] AddQuoteLineBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new AddQuoteLineBffCommand(
            GetToken(), id, req.CatalogItemId, req.SKU, req.ItemName,
            req.Quantity, req.UnitPrice, req.CurrencyCode, req.DiscountPercent, req.Notes), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new SendQuoteBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new AcceptQuoteBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new RejectQuoteBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/convert-to-order")]
    public async Task<IActionResult> ConvertToOrder(
        Guid id, [FromBody] ConvertToOrderBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new ConvertQuoteToOrderBffCommand(
            GetToken(), id, req.RequestedDeliveryDate, req.Notes), ct);
        return Created(string.Empty, result);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

// ── Orders ────────────────────────────────────────────────────────────────────

[ApiController]
[Route("bff/sales/orders")]
[Authorize]
public class SalesOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new SearchSalesOrdersBffQuery(GetToken(), customerId, status, skip, take), ct));

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ConfirmSalesOrderBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelOrderBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new CancelSalesOrderBffCommand(GetToken(), id, req.Reason), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateQuoteBffRequest(
    Guid CustomerId,
    DateOnly ValidUntil,
    string CurrencyCode,
    string CountryCode,
    string? Notes);

public record AddQuoteLineBffRequest(
    Guid CatalogItemId,
    string SKU,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent = 0,
    string? Notes = null);

public record ConvertToOrderBffRequest(
    DateOnly? RequestedDeliveryDate = null,
    string? Notes = null);

public record CancelOrderBffRequest(string Reason);
