using BFF.Application.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/inventory/items")]
[Authorize]
public class InventoryItemsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? warehouseId,
        [FromQuery] string? sku,
        [FromQuery] bool? belowReorderPoint,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new SearchInventoryBffQuery(GetToken(), warehouseId, sku, belowReorderPoint, skip, take), ct));

    [HttpPost("receive")]
    public async Task<IActionResult> Receive([FromBody] ReceiveStockBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new ReceiveStockBffCommand(
            GetToken(), req.CatalogItemId, req.WarehouseId, req.SKU,
            req.Quantity, req.ReferenceNumber, req.Notes, req.ReorderPoint), ct));

    [HttpPost("{id:guid}/adjust")]
    public async Task<IActionResult> Adjust(
        Guid id, [FromBody] AdjustStockBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new AdjustStockBffCommand(
            GetToken(), id, req.Delta, req.Reason, req.Notes), ct));

    [HttpPost("{id:guid}/write-off")]
    public async Task<IActionResult> WriteOff(
        Guid id, [FromBody] WriteOffStockBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new WriteOffStockBffCommand(
            GetToken(), id, req.Quantity, req.Reason, req.Notes), ct));

    [HttpGet("{id:guid}/movements")]
    public async Task<IActionResult> GetMovements(
        Guid id,
        [FromQuery] string? type,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new GetStockMovementsBffQuery(GetToken(), id, type, skip, take), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

[ApiController]
[Route("bff/inventory/warehouses")]
[Authorize]
public class InventoryWarehousesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await mediator.Send(new ListWarehousesBffQuery(GetToken()), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateWarehouseBffCommand(
            GetToken(), req.Code, req.Name, req.Address), ct);
        return Created(string.Empty, result);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record ReceiveStockBffRequest(
    Guid CatalogItemId, Guid WarehouseId, string SKU,
    decimal Quantity, string? ReferenceNumber, string? Notes, decimal? ReorderPoint);
public record AdjustStockBffRequest(decimal Delta, string Reason, string? Notes);
public record WriteOffStockBffRequest(decimal Quantity, string Reason, string? Notes);
public record CreateWarehouseBffRequest(string Code, string Name, string? Address);
