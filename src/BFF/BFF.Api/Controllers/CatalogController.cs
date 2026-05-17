using BFF.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

// ── Catalog Items ─────────────────────────────────────────────────────────────

[ApiController]
[Route("bff/catalog/items")]
[Authorize]
public class CatalogItemsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? sku,
        [FromQuery] string? itemType,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchCatalogItemsBffQuery(GetToken(), name, sku, itemType, isActive, skip, take), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCatalogItemBffRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCatalogItemBffCommand(
            GetToken(),
            request.SKU, request.Name, request.Description,
            request.ItemType, request.UnitOfMeasure, request.TaxCategory,
            request.DefaultCurrency, request.CountryCode,
            request.TrackInventory, request.ReorderPoint), ct);
        return Created(string.Empty, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateCatalogItemBffCommand(GetToken(), id), ct);
        return NoContent();
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

// ── Price Lists ───────────────────────────────────────────────────────────────

[ApiController]
[Route("bff/catalog/pricelists")]
[Authorize]
public class CatalogPriceListsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await mediator.Send(new ListPriceListsBffQuery(GetToken()), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePriceListBffRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreatePriceListBffCommand(
            GetToken(), request.Name, request.CurrencyCode,
            request.ValidFrom, request.ValidTo), ct);
        return Created(string.Empty, result);
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SetDefaultPriceListBffCommand(GetToken(), id), ct);
        return NoContent();
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateCatalogItemBffRequest(
    string SKU,
    string Name,
    string? Description,
    string ItemType,
    string UnitOfMeasure,
    string TaxCategory,
    string DefaultCurrency,
    string CountryCode,
    bool TrackInventory = false,
    decimal? ReorderPoint = null);

public record CreatePriceListBffRequest(
    string Name,
    string CurrencyCode,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
