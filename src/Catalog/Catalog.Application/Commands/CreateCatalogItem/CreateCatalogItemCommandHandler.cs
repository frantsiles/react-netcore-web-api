using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Catalog;
using Catalog.Domain.Repositories;
using Catalog.Domain.ValueObjects;
using MediatR;

namespace Catalog.Application.Commands.CreateCatalogItem;

public class CreateCatalogItemCommandHandler(
    ICatalogItemRepository repository,
    IEventPublisher eventPublisher) : IRequestHandler<CreateCatalogItemCommand, CatalogItemDto>
{
    public async Task<CatalogItemDto> Handle(CreateCatalogItemCommand request, CancellationToken ct)
    {
        if (await repository.ExistsBySKUAsync(request.SKU, ct))
            throw new DomainException($"A catalog item with SKU '{request.SKU}' already exists.");

        var item = CatalogItem.Create(
            request.SKU, request.Name, request.Description,
            request.ItemType, UnitOfMeasure.Create(request.UnitOfMeasure),
            request.TaxCategory, request.DefaultCurrency, request.CountryCode,
            request.TrackInventory, request.ReorderPoint);

        await repository.AddAsync(item, ct);

        foreach (var e in item.DomainEvents) await eventPublisher.PublishAsync(e, ct);
        item.ClearDomainEvents();

        return item.ToDto();
    }
}
