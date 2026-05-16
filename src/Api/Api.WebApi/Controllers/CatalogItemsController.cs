using Catalog.Application.Commands.CreateCatalogItem;
using Catalog.Application.Commands.DeactivateCatalogItem;
using Catalog.Application.Commands.UpdateCatalogItem;
using Catalog.Application.Queries.GetCatalogItemById;
using Catalog.Application.Queries.GetCatalogItemBySKU;
using Catalog.Application.Queries.GetItemPrice;
using Catalog.Application.Queries.SearchCatalogItems;
using Catalog.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/catalog/items")]
[Authorize]
public class CatalogItemsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCatalogItemCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.CatalogItemId }, result);
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? name, [FromQuery] string? sku,
        [FromQuery] ItemType? itemType, [FromQuery] bool? isActive,
        [FromQuery] int skip = 0, [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchCatalogItemsQuery(name, sku, itemType, isActive, skip, take), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetCatalogItemByIdQuery(id), ct));

    [HttpGet("by-sku/{sku}")]
    public async Task<IActionResult> GetBySKU(string sku, CancellationToken ct)
        => Ok(await mediator.Send(new GetCatalogItemBySKUQuery(sku), ct));

    [HttpGet("{id:guid}/price")]
    public async Task<IActionResult> GetPrice(
        Guid id, [FromQuery] decimal quantity = 1,
        [FromQuery] DateOnly? onDate = null, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetItemPriceQuery(id, quantity, onDate), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCatalogItemRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateCatalogItemCommand(id, request.Name, request.Description,
                request.UnitOfMeasure, request.TaxCategory), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateCatalogItemCommand(id), ct);
        return NoContent();
    }
}

public record UpdateCatalogItemRequest(
    string Name, string? Description, string UnitOfMeasure, string TaxCategory);
