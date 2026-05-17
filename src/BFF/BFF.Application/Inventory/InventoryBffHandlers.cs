using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Inventory;

public class ListWarehousesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListWarehousesBffQuery, IReadOnlyList<WarehouseBffDto>>
{
    public async Task<IReadOnlyList<WarehouseBffDto>> Handle(
        ListWarehousesBffQuery request, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<List<WarehouseBffDto>>("api/inventory/warehouses", request.Token, ct);
        return result ?? [];
    }
}

public class SearchInventoryBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchInventoryBffQuery, IReadOnlyList<InventoryItemBffDto>>
{
    public async Task<IReadOnlyList<InventoryItemBffDto>> Handle(
        SearchInventoryBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (request.WarehouseId.HasValue) parts.Add($"warehouseId={request.WarehouseId}");
        if (!string.IsNullOrWhiteSpace(request.Sku)) parts.Add($"sku={Uri.EscapeDataString(request.Sku)}");
        if (request.BelowReorderPoint.HasValue) parts.Add($"belowReorderPoint={request.BelowReorderPoint}");
        parts.Add($"skip={request.Skip}");
        parts.Add($"take={request.Take}");

        var url = $"api/inventory/items?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<InventoryItemBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class GetStockMovementsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetStockMovementsBffQuery, IReadOnlyList<StockMovementBffDto>>
{
    public async Task<IReadOnlyList<StockMovementBffDto>> Handle(
        GetStockMovementsBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Type)) parts.Add($"type={request.Type}");
        parts.Add($"skip={request.Skip}");
        parts.Add($"take={request.Take}");

        var url = $"api/inventory/items/{request.ItemId}/movements?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<StockMovementBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class ReceiveStockBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ReceiveStockBffCommand, InventoryItemBffDto>
{
    public async Task<InventoryItemBffDto> Handle(ReceiveStockBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            CatalogItemId  = request.CatalogItemId,
            WarehouseId    = request.WarehouseId,
            SKU            = request.SKU,
            Quantity       = request.Quantity,
            ReferenceNumber = request.ReferenceNumber,
            Notes          = request.Notes,
            ReorderPoint   = request.ReorderPoint,
        };
        var result = await apiClient.PostAsync<object, InventoryItemBffDto>(
            "api/inventory/items/receive", body, request.Token, ct);
        return result!;
    }
}

public class AdjustStockBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<AdjustStockBffCommand, InventoryItemBffDto>
{
    public async Task<InventoryItemBffDto> Handle(AdjustStockBffCommand request, CancellationToken ct)
    {
        var body = new { Delta = request.Delta, Reason = request.Reason, Notes = request.Notes };
        var result = await apiClient.PostAsync<object, InventoryItemBffDto>(
            $"api/inventory/items/{request.ItemId}/adjust", body, request.Token, ct);
        return result!;
    }
}

public class WriteOffStockBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<WriteOffStockBffCommand, InventoryItemBffDto>
{
    public async Task<InventoryItemBffDto> Handle(WriteOffStockBffCommand request, CancellationToken ct)
    {
        var body = new { Quantity = request.Quantity, Reason = request.Reason, Notes = request.Notes };
        var result = await apiClient.PostAsync<object, InventoryItemBffDto>(
            $"api/inventory/items/{request.ItemId}/write-off", body, request.Token, ct);
        return result!;
    }
}

public class CreateWarehouseBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateWarehouseBffCommand, WarehouseBffDto>
{
    public async Task<WarehouseBffDto> Handle(CreateWarehouseBffCommand request, CancellationToken ct)
    {
        var body = new { Code = request.Code, Name = request.Name, Address = request.Address };
        var result = await apiClient.PostAsync<object, WarehouseBffDto>(
            "api/inventory/warehouses", body, request.Token, ct);
        return result!;
    }
}
