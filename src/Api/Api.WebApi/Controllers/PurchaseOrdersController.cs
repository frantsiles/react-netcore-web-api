using Api.WebApi.Infrastructure.Pdf;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parties.Application.Queries.GetPartyById;
using Purchasing.Application.PurchaseOrders.Commands.AddPurchaseOrderLine;
using Purchasing.Application.PurchaseOrders.Commands.CancelPurchaseOrder;
using Purchasing.Application.PurchaseOrders.Commands.ConfirmPurchaseOrder;
using Purchasing.Application.PurchaseOrders.Commands.CreatePurchaseOrder;
using Purchasing.Application.PurchaseOrders.Commands.ReceivePurchaseOrder;
using Purchasing.Application.PurchaseOrders.Commands.RemovePurchaseOrderLine;
using Purchasing.Application.PurchaseOrders.Commands.SendPurchaseOrder;
using Purchasing.Application.PurchaseOrders.Commands.UpdatePurchaseOrderLine;
using Purchasing.Application.PurchaseOrders.Queries.GetPurchaseOrderById;
using Purchasing.Application.PurchaseOrders.Queries.SearchPurchaseOrders;
using Purchasing.Domain.PurchaseOrders;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/purchasing/orders")]
[Authorize]
public class PurchaseOrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? supplierId,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] string? poNumber,
        CancellationToken ct)
        => Ok(await sender.Send(new SearchPurchaseOrdersQuery(supplierId, status, poNumber), ct));

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] AddPoLineRequest req, CancellationToken ct)
        => Ok(await sender.Send(new AddPurchaseOrderLineCommand(
            id, req.CatalogItemId, req.Sku, req.ItemName, req.QuantityOrdered, req.UnitCostAmount, req.Notes), ct));

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId,
        [FromBody] UpdateLineRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdatePurchaseOrderLineCommand(
            id, lineId, req.QuantityOrdered, req.UnitCostAmount, req.Notes), ct));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct)
        => Ok(await sender.Send(new RemovePurchaseOrderLineCommand(id, lineId), ct));

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new SendPurchaseOrderCommand(id), ct));

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new ConfirmPurchaseOrderCommand(id), ct));

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceiveRequest req, CancellationToken ct)
        => Ok(await sender.Send(new ReceivePurchaseOrderCommand(
            id, req.LineId, req.WarehouseId, req.QuantityReceived), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest req, CancellationToken ct)
        => Ok(await sender.Send(new CancelPurchaseOrderCommand(id, req.Reason), ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var po = await sender.Send(new GetPurchaseOrderByIdQuery(id), ct);
        var supplier = await sender.Send(new GetPartyByIdQuery(po.SupplierId), ct);
        var bytes = PdfService.GeneratePurchaseOrderPdf(po, supplier);
        return File(bytes, "application/pdf", $"OC-{po.PoNumber}.pdf");
    }
}

public record AddPoLineRequest(
    Guid CatalogItemId, string Sku, string ItemName,
    decimal QuantityOrdered, decimal UnitCostAmount, string? Notes);
public record UpdateLineRequest(decimal QuantityOrdered, decimal UnitCostAmount, string? Notes);
public record ReceiveRequest(Guid LineId, Guid WarehouseId, decimal QuantityReceived);
public record CancelRequest(string Reason);
