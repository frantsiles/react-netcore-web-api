using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Queries.SearchCatalogItems;

public class SearchCatalogItemsQueryHandler(ICatalogItemRepository repository)
    : IRequestHandler<SearchCatalogItemsQuery, IReadOnlyList<CatalogItemDto>>
{
    public async Task<IReadOnlyList<CatalogItemDto>> Handle(SearchCatalogItemsQuery request, CancellationToken ct)
    {
        var items = await repository.SearchAsync(
            request.Name, request.SKU, request.ItemType,
            request.IsActive, request.Skip, request.Take, ct);
        return items.Select(i => i.ToDto()).ToList();
    }
}
