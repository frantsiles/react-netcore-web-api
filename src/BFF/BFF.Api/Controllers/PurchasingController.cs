using BFF.Application.Purchasing;
using BFF.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/purchasing/orders")]
[Authorize]
public class PurchasingController(IMediator mediator, IApiClient apiClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? supplierId,
        [FromQuery] string? status,
        [FromQuery] string? poNumber,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new SearchPurchaseOrdersBffQuery(GetToken(), supplierId, status, poNumber, skip, take), ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePoBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreatePurchaseOrderBffCommand(
            GetToken(), req.SupplierId, req.CurrencyCode, req.CountryCode,
            req.ExpectedDeliveryDate, req.Notes), ct);
        return Created(string.Empty, result);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(
        Guid id, [FromBody] AddPoLineBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new AddPoLineBffCommand(
            GetToken(), id, req.CatalogItemId, req.Sku, req.ItemName,
            req.QuantityOrdered, req.UnitCostAmount, req.Notes), ct));

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new SendPurchaseOrderBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ConfirmPurchaseOrderBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(
        Guid id, [FromBody] ReceivePoLineBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new ReceivePurchaseOrderBffCommand(
            GetToken(), id, req.LineId, req.WarehouseId, req.QuantityReceived), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelPoBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new CancelPurchaseOrderBffCommand(GetToken(), id, req.Reason), ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var (bytes, contentType, fileName) = await apiClient.GetFileAsync(
            $"api/purchasing/orders/{id}/pdf", GetToken(), ct);
        return File(bytes, contentType, fileName);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreatePoBffRequest(
    Guid SupplierId,
    string CurrencyCode,
    string CountryCode,
    DateTime? ExpectedDeliveryDate = null,
    string? Notes = null);

public record AddPoLineBffRequest(
    Guid CatalogItemId,
    string Sku,
    string ItemName,
    decimal QuantityOrdered,
    decimal UnitCostAmount,
    string? Notes = null);

public record ReceivePoLineBffRequest(
    Guid LineId,
    Guid WarehouseId,
    decimal QuantityReceived);

public record CancelPoBffRequest(string Reason);
