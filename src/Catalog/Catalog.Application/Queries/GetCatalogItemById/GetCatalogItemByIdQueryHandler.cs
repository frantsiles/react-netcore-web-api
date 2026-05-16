using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Queries.GetCatalogItemById;

public class GetCatalogItemByIdQueryHandler(ICatalogItemRepository repository)
    : IRequestHandler<GetCatalogItemByIdQuery, CatalogItemDto>
{
    public async Task<CatalogItemDto> Handle(GetCatalogItemByIdQuery request, CancellationToken ct)
    {
        var item = await repository.GetByIdAsync(request.CatalogItemId, ct)
            ?? throw new DomainException($"Catalog item '{request.CatalogItemId}' not found.");
        return item.ToDto();
    }
}
