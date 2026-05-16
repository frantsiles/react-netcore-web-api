using Catalog.Application.Common.Dtos;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Queries.GetItemPrice;

public class GetItemPriceQueryHandler(
    ICatalogItemRepository itemRepository,
    IPriceListRepository priceListRepository)
    : IRequestHandler<GetItemPriceQuery, ItemPriceDto?>
{
    public async Task<ItemPriceDto?> Handle(GetItemPriceQuery request, CancellationToken ct)
    {
        var item = await itemRepository.GetByIdAsync(request.CatalogItemId, ct);
        if (item is null) return null;

        var date = request.OnDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var activeLists = await priceListRepository.GetActiveOnDateAsync(date, ct);

        // Prefer default price list; otherwise first active
        var priceList = activeLists.FirstOrDefault(pl => pl.IsDefault)
            ?? activeLists.FirstOrDefault();

        if (priceList is null) return null;

        // Find best applicable entry: highest MinQuantity <= requested quantity
        var entry = priceList.Entries
            .Where(e => e.CatalogItemId == request.CatalogItemId && e.MinQuantity <= request.Quantity)
            .OrderByDescending(e => e.MinQuantity)
            .FirstOrDefault();

        if (entry is null) return null;

        return new ItemPriceDto(
            item.Id, item.SKU,
            entry.UnitPrice.Amount, entry.UnitPrice.CurrencyCode,
            entry.MinQuantity, priceList.Name);
    }
}
