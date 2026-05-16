using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using Catalog.Domain.ValueObjects;
using MediatR;

namespace Catalog.Application.Commands.UpdateCatalogItem;

public class UpdateCatalogItemCommandHandler(ICatalogItemRepository repository)
    : IRequestHandler<UpdateCatalogItemCommand, CatalogItemDto>
{
    public async Task<CatalogItemDto> Handle(UpdateCatalogItemCommand request, CancellationToken ct)
    {
        var item = await repository.GetByIdAsync(request.CatalogItemId, ct)
            ?? throw new DomainException($"Catalog item '{request.CatalogItemId}' not found.");

        item.Update(request.Name, request.Description,
            UnitOfMeasure.Create(request.UnitOfMeasure), request.TaxCategory);
        await repository.UpdateAsync(item, ct);

        return item.ToDto();
    }
}
