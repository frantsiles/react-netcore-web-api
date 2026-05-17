using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Purchasing;

public class SearchPurchaseOrdersBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchPurchaseOrdersBffQuery, IReadOnlyList<PurchaseOrderBffDto>>
{
    public async Task<IReadOnlyList<PurchaseOrderBffDto>> Handle(
        SearchPurchaseOrdersBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (request.SupplierId.HasValue)
            parts.Add($"supplierId={request.SupplierId}");
        if (!string.IsNullOrWhiteSpace(request.Status))
            parts.Add($"status={request.Status}");
        if (!string.IsNullOrWhiteSpace(request.PoNumber))
            parts.Add($"poNumber={Uri.EscapeDataString(request.PoNumber)}");

        var url = $"api/purchasing/orders?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<PurchaseOrderBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class CreatePurchaseOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreatePurchaseOrderBffCommand, PurchaseOrderBffDto>
{
    public async Task<PurchaseOrderBffDto> Handle(
        CreatePurchaseOrderBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            SupplierId           = request.SupplierId,
            CurrencyCode         = request.CurrencyCode,
            CountryCode          = request.CountryCode,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes                = request.Notes,
        };
        var result = await apiClient.PostAsync<object, PurchaseOrderBffDto>(
            "api/purchasing/orders", body, request.Token, ct);
        return result!;
    }
}

public class AddPoLineBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<AddPoLineBffCommand, PurchaseOrderBffDto>
{
    public async Task<PurchaseOrderBffDto> Handle(
        AddPoLineBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            CatalogItemId  = request.CatalogItemId,
            Sku            = request.Sku,
            ItemName       = request.ItemName,
            QuantityOrdered = request.QuantityOrdered,
            UnitCostAmount = request.UnitCostAmount,
            Notes          = request.Notes,
        };
        var result = await apiClient.PostAsync<object, PurchaseOrderBffDto>(
            $"api/purchasing/orders/{request.OrderId}/lines", body, request.Token, ct);
        return result!;
    }
}

public class SendPurchaseOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<SendPurchaseOrderBffCommand, PurchaseOrderBffDto>
{
    public async Task<PurchaseOrderBffDto> Handle(
        SendPurchaseOrderBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, PurchaseOrderBffDto>(
            $"api/purchasing/orders/{request.OrderId}/send", new { }, request.Token, ct);
        return result!;
    }
}

public class ConfirmPurchaseOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ConfirmPurchaseOrderBffCommand, PurchaseOrderBffDto>
{
    public async Task<PurchaseOrderBffDto> Handle(
        ConfirmPurchaseOrderBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, PurchaseOrderBffDto>(
            $"api/purchasing/orders/{request.OrderId}/confirm", new { }, request.Token, ct);
        return result!;
    }
}

public class ReceivePurchaseOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ReceivePurchaseOrderBffCommand, PurchaseOrderBffDto>
{
    public async Task<PurchaseOrderBffDto> Handle(
        ReceivePurchaseOrderBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            LineId           = request.LineId,
            WarehouseId      = request.WarehouseId,
            QuantityReceived = request.QuantityReceived,
        };
        var result = await apiClient.PostAsync<object, PurchaseOrderBffDto>(
            $"api/purchasing/orders/{request.OrderId}/receive", body, request.Token, ct);
        return result!;
    }
}

public class CancelPurchaseOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CancelPurchaseOrderBffCommand, PurchaseOrderBffDto>
{
    public async Task<PurchaseOrderBffDto> Handle(
        CancelPurchaseOrderBffCommand request, CancellationToken ct)
    {
        var body = new { Reason = request.Reason };
        var result = await apiClient.PostAsync<object, PurchaseOrderBffDto>(
            $"api/purchasing/orders/{request.OrderId}/cancel", body, request.Token, ct);
        return result!;
    }
}
