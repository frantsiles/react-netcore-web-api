using MediatR;

namespace BFF.Application.Catalog;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record CatalogItemBffDto(
    Guid CatalogItemId,
    string SKU,
    string Name,
    string? Description,
    string ItemType,
    string UnitOfMeasure,
    string TaxCategory,
    string DefaultCurrency,
    string CountryCode,
    bool IsActive,
    bool TrackInventory,
    decimal? ReorderPoint);

public record PriceListBffDto(
    Guid PriceListId,
    string Name,
    string CurrencyCode,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsDefault,
    string Status);

// ── Queries ───────────────────────────────────────────────────────────────────

public record SearchCatalogItemsBffQuery(
    string BearerToken,
    string? Name,
    string? Sku,
    string? ItemType,
    bool? IsActive,
    int Skip,
    int Take) : IRequest<IReadOnlyList<CatalogItemBffDto>>;

public record ListPriceListsBffQuery(
    string BearerToken) : IRequest<IReadOnlyList<PriceListBffDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateCatalogItemBffCommand(
    string BearerToken,
    string SKU,
    string Name,
    string? Description,
    string ItemType,           // "Product" | "Service"
    string UnitOfMeasure,
    string TaxCategory,
    string DefaultCurrency,
    string CountryCode,
    bool TrackInventory,
    decimal? ReorderPoint) : IRequest<CatalogItemBffDto>;

public record DeactivateCatalogItemBffCommand(
    string BearerToken,
    Guid ItemId) : IRequest<Unit>;

public record CreatePriceListBffCommand(
    string BearerToken,
    string Name,
    string CurrencyCode,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : IRequest<PriceListBffDto>;

public record SetDefaultPriceListBffCommand(
    string BearerToken,
    Guid PriceListId) : IRequest<Unit>;
