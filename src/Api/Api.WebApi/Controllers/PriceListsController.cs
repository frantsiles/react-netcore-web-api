using Catalog.Application.Commands.AddPriceEntry;
using Catalog.Application.Commands.CreatePriceList;
using Catalog.Application.Commands.RemovePriceEntry;
using Catalog.Application.Commands.SetDefaultPriceList;
using Catalog.Application.Commands.UpdatePriceEntry;
using Catalog.Application.Commands.UpdatePriceList;
using Catalog.Application.Queries.GetPriceListById;
using Catalog.Application.Queries.ListPriceLists;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/catalog/pricelists")]
[Authorize]
public class PriceListsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePriceListCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.PriceListId }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await mediator.Send(new ListPriceListsQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPriceListByIdQuery(id), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePriceListRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdatePriceListCommand(id, request.Name, request.ValidFrom, request.ValidTo), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/entries")]
    public async Task<IActionResult> AddEntry(Guid id, [FromBody] AddPriceEntryRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new AddPriceEntryCommand(id, request.CatalogItemId, request.UnitPrice, request.MinQuantity), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(
        Guid id, Guid entryId, [FromBody] UpdatePriceEntryRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdatePriceEntryCommand(id, entryId, request.UnitPrice, request.MinQuantity), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> RemoveEntry(Guid id, Guid entryId, CancellationToken ct)
    {
        var result = await mediator.Send(new RemovePriceEntryCommand(id, entryId), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SetDefaultPriceListCommand(id), ct);
        return NoContent();
    }
}

public record UpdatePriceListRequest(string Name, DateOnly ValidFrom, DateOnly? ValidTo);
public record AddPriceEntryRequest(Guid CatalogItemId, decimal UnitPrice, decimal MinQuantity = 1m);
public record UpdatePriceEntryRequest(decimal UnitPrice, decimal MinQuantity);
