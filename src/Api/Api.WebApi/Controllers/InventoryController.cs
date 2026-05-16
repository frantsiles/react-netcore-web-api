using Inventory.Application.Inventory.Commands.AdjustStock;
using Inventory.Application.Inventory.Commands.ReceiveStock;
using Inventory.Application.Inventory.Commands.ReleaseReservation;
using Inventory.Application.Inventory.Commands.ReserveStock;
using Inventory.Application.Inventory.Commands.WriteOffStock;
using Inventory.Application.Inventory.Queries.GetInventoryItem;
using Inventory.Application.Inventory.Queries.GetStockMovements;
using Inventory.Application.Inventory.Queries.SearchInventory;
using Inventory.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/inventory/items")]
[Authorize]
public class InventoryController(IMediator mediator) : ControllerBase
{
    [HttpPost("receive")]
    public async Task<IActionResult> Receive([FromBody] ReceiveStockCommand command,
        CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetInventoryItemQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? warehouseId,
        [FromQuery] string? sku,
        [FromQuery] bool? belowReorderPoint,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(
            new SearchInventoryQuery(warehouseId, sku, belowReorderPoint, skip, take), ct));

    [HttpGet("{id:guid}/movements")]
    public async Task<IActionResult> GetMovements(
        Guid id,
        [FromQuery] MovementType? type,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetStockMovementsQuery(id, type, skip, take), ct));

    [HttpPost("{id:guid}/adjust")]
    public async Task<IActionResult> Adjust(Guid id,
        [FromBody] AdjustStockRequest body, CancellationToken ct) =>
        Ok(await mediator.Send(new AdjustStockCommand(id, body.Delta, body.Reason, body.Notes), ct));

    [HttpPost("{id:guid}/write-off")]
    public async Task<IActionResult> WriteOff(Guid id,
        [FromBody] WriteOffStockRequest body, CancellationToken ct) =>
        Ok(await mediator.Send(new WriteOffStockCommand(id, body.Quantity, body.Reason, body.Notes), ct));

    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveStockCommand command,
        CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPost("release-reservation")]
    public async Task<IActionResult> ReleaseReservation(
        [FromBody] ReleaseReservationCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));
}

public record AdjustStockRequest(decimal Delta, string Reason, string? Notes = null);
public record WriteOffStockRequest(decimal Quantity, string Reason, string? Notes = null);
