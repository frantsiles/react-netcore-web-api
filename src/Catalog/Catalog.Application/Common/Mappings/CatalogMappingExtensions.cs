using Catalog.Application.Common.Dtos;
using Catalog.Domain.Catalog;

namespace Catalog.Application.Common.Mappings;

public static class CatalogMappingExtensions
{
    public static CatalogItemDto ToDto(this CatalogItem item) => new(
        item.Id,
        item.SKU,
        item.Name,
        item.Description,
        item.ItemType.ToString(),
        item.UnitOfMeasure.Code,
        item.TaxCategory,
        item.DefaultCurrency,
        item.CountryCode,
        item.IsActive,
        item.TrackInventory,
        item.ReorderPoint);

    public static PriceListDto ToDto(this PriceList pl) => new(
        pl.Id,
        pl.Name,
        pl.CurrencyCode,
        pl.ValidFrom,
        pl.ValidTo,
        pl.IsDefault,
        pl.Entries.Select(e => new PriceListEntryDto(
            e.Id, e.CatalogItemId, e.UnitPrice.Amount, e.UnitPrice.CurrencyCode, e.MinQuantity))
            .ToList());

    public static PriceListSummaryDto ToSummaryDto(this PriceList pl)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var status = pl.ValidFrom > today ? "Upcoming"
            : pl.ValidTo.HasValue && pl.ValidTo.Value < today ? "Expired"
            : "Active";

        return new(pl.Id, pl.Name, pl.CurrencyCode, pl.ValidFrom, pl.ValidTo, pl.IsDefault, status);
    }
}
