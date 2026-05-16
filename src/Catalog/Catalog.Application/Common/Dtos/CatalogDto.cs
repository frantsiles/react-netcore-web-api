namespace Catalog.Application.Common.Dtos;

public record CatalogItemDto(
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

public record PriceListDto(
    Guid PriceListId,
    string Name,
    string CurrencyCode,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsDefault,
    IReadOnlyList<PriceListEntryDto> Entries);

public record PriceListSummaryDto(
    Guid PriceListId,
    string Name,
    string CurrencyCode,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsDefault,
    string Status);

public record PriceListEntryDto(
    Guid EntryId,
    Guid CatalogItemId,
    decimal UnitPrice,
    string CurrencyCode,
    decimal MinQuantity);

public record ItemPriceDto(
    Guid CatalogItemId,
    string SKU,
    decimal UnitPrice,
    string CurrencyCode,
    decimal AppliedMinQuantity,
    string PriceListName);
