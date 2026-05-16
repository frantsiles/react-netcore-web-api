using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Queries.GetCatalogItemBySKU;

public class GetCatalogItemBySKUQueryHandler(ICatalogItemRepository repository)
    : IRequestHandler<GetCatalogItemBySKUQuery, CatalogItemDto>
{
    public async Task<CatalogItemDto> Handle(GetCatalogItemBySKUQuery request, CancellationToken ct)
    {
        var item = await repository.GetBySKUAsync(request.SKU.ToUpperInvariant(), ct)
            ?? throw new DomainException($"Catalog item with SKU '{request.SKU}' not found.");
        return item.ToDto();
    }
}
