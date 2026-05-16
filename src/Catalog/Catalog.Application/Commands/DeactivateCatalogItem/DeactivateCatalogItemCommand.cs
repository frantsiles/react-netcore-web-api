using MediatR;

namespace Catalog.Application.Commands.DeactivateCatalogItem;

public record DeactivateCatalogItemCommand(Guid CatalogItemId) : IRequest;
