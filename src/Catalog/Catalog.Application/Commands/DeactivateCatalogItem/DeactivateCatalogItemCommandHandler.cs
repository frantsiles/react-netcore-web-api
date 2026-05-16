using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Commands.DeactivateCatalogItem;

public class DeactivateCatalogItemCommandHandler(
    ICatalogItemRepository repository,
    IEventPublisher eventPublisher) : IRequestHandler<DeactivateCatalogItemCommand>
{
    public async Task Handle(DeactivateCatalogItemCommand request, CancellationToken ct)
    {
        var item = await repository.GetByIdAsync(request.CatalogItemId, ct)
            ?? throw new DomainException($"Catalog item '{request.CatalogItemId}' not found.");

        item.Deactivate();
        await repository.UpdateAsync(item, ct);

        foreach (var e in item.DomainEvents) await eventPublisher.PublishAsync(e, ct);
        item.ClearDomainEvents();
    }
}
