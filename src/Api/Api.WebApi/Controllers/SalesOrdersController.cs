using Api.WebApi.Infrastructure.Pdf;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parties.Application.Queries.GetPartyById;
using Sales.Application.Orders.Commands.AddOrderLine;
using Sales.Application.Orders.Commands.CancelSalesOrder;
using Sales.Application.Orders.Commands.ConfirmSalesOrder;
using Sales.Application.Orders.Commands.CreateSalesOrder;
using Sales.Application.Orders.Commands.RemoveOrderLine;
using Sales.Application.Orders.Commands.UpdateOrderLine;
using Sales.Application.Orders.Queries.GetSalesOrderById;
using Sales.Application.Orders.Queries.SearchSalesOrders;
using Sales.Domain.Orders;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/sales/orders")]
[Authorize]
public class SalesOrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSalesOrderCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetSalesOrderByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? customerId,
        [FromQuery] SalesOrderStatus? status,
        [FromQuery] Guid? originQuoteId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new SearchSalesOrdersQuery(customerId, status, originQuoteId, skip, take), ct));

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(Guid id,
        [FromBody] AddOrderLineRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new AddOrderLineCommand(id,
            body.CatalogItemId, body.SKU, body.ItemName, body.Quantity,
            body.UnitPrice, body.CurrencyCode, body.DiscountPercent, body.Notes), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId,
        [FromBody] UpdateOrderLineRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateOrderLineCommand(id, lineId,
            body.Quantity, body.UnitPrice, body.CurrencyCode, body.DiscountPercent, body.Notes), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct) =>
        Ok(await mediator.Send(new RemoveOrderLineCommand(id, lineId), ct));

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new ConfirmSalesOrderCommand(id), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id,
        [FromBody] CancelOrderRequest body, CancellationToken ct) =>
        Ok(await mediator.Send(new CancelSalesOrderCommand(id, body.Reason), ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var order = await mediator.Send(new GetSalesOrderByIdQuery(id), ct);
        var customer = await mediator.Send(new GetPartyByIdQuery(order.CustomerId), ct);
        var bytes = PdfService.GenerateSalesOrderPdf(order, customer);
        return File(bytes, "application/pdf", $"OV-{order.OrderNumber}.pdf");
    }
}

public record AddOrderLineRequest(
    Guid CatalogItemId, string SKU, string ItemName,
    decimal Quantity, decimal UnitPrice, string CurrencyCode,
    decimal DiscountPercent = 0, string? Notes = null);

public record UpdateOrderLineRequest(
    decimal Quantity, decimal UnitPrice, string CurrencyCode,
    decimal DiscountPercent = 0, string? Notes = null);

public record CancelOrderRequest(string Reason);
