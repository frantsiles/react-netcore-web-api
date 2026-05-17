using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Catalog;

public class SearchCatalogItemsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchCatalogItemsBffQuery, IReadOnlyList<CatalogItemBffDto>>
{
    public async Task<IReadOnlyList<CatalogItemBffDto>> Handle(
        SearchCatalogItemsBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Name))
            parts.Add($"name={Uri.EscapeDataString(request.Name)}");
        if (!string.IsNullOrWhiteSpace(request.Sku))
            parts.Add($"sku={Uri.EscapeDataString(request.Sku)}");
        if (!string.IsNullOrWhiteSpace(request.ItemType))
            parts.Add($"itemType={request.ItemType}");
        if (request.IsActive is not null)
            parts.Add($"isActive={request.IsActive.ToString()!.ToLower()}");
        parts.Add($"skip={request.Skip}");
        parts.Add($"take={request.Take}");

        var url = $"api/catalog/items?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<CatalogItemBffDto>>(url, request.BearerToken, ct);
        return result ?? [];
    }
}

public class ListPriceListsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListPriceListsBffQuery, IReadOnlyList<PriceListBffDto>>
{
    public async Task<IReadOnlyList<PriceListBffDto>> Handle(
        ListPriceListsBffQuery request, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<List<PriceListBffDto>>(
            "api/catalog/pricelists", request.BearerToken, ct);
        return result ?? [];
    }
}

public class CreateCatalogItemBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateCatalogItemBffCommand, CatalogItemBffDto>
{
    public async Task<CatalogItemBffDto> Handle(
        CreateCatalogItemBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            SKU           = request.SKU,
            Name          = request.Name,
            Description   = request.Description,
            ItemType      = request.ItemType,
            UnitOfMeasure = request.UnitOfMeasure,
            TaxCategory   = request.TaxCategory,
            DefaultCurrency = request.DefaultCurrency,
            CountryCode   = request.CountryCode,
            TrackInventory = request.TrackInventory,
            ReorderPoint  = request.ReorderPoint,
        };
        var result = await apiClient.PostAsync<object, CatalogItemBffDto>(
            "api/catalog/items", body, request.BearerToken, ct);
        return result!;
    }
}

public class DeactivateCatalogItemBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<DeactivateCatalogItemBffCommand, Unit>
{
    public async Task<Unit> Handle(
        DeactivateCatalogItemBffCommand request, CancellationToken ct)
    {
        await apiClient.DeleteAsync($"api/catalog/items/{request.ItemId}", request.BearerToken, ct);
        return Unit.Value;
    }
}

public class CreatePriceListBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreatePriceListBffCommand, PriceListBffDto>
{
    public async Task<PriceListBffDto> Handle(
        CreatePriceListBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            Name         = request.Name,
            CurrencyCode = request.CurrencyCode,
            ValidFrom    = request.ValidFrom,
            ValidTo      = request.ValidTo,
        };
        var result = await apiClient.PostAsync<object, PriceListBffDto>(
            "api/catalog/pricelists", body, request.BearerToken, ct);
        return result!;
    }
}

public class SetDefaultPriceListBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<SetDefaultPriceListBffCommand, Unit>
{
    public async Task<Unit> Handle(
        SetDefaultPriceListBffCommand request, CancellationToken ct)
    {
        await apiClient.PostAsync<object, object>(
            $"api/catalog/pricelists/{request.PriceListId}/set-default",
            new { }, request.BearerToken, ct);
        return Unit.Value;
    }
}
