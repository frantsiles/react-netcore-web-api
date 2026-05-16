using Catalog.Application.Common.Dtos;
using Catalog.Domain.Catalog;
using MediatR;

namespace Catalog.Application.Queries.SearchCatalogItems;

public record SearchCatalogItemsQuery(
    string? Name,
    string? SKU,
    ItemType? ItemType,
    bool? IsActive,
    int Skip = 0,
    int Take = 20) : IRequest<IReadOnlyList<CatalogItemDto>>;
