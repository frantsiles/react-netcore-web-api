using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Commands.UpdateCatalogItem;

public record UpdateCatalogItemCommand(
    Guid CatalogItemId,
    string Name,
    string? Description,
    string UnitOfMeasure,
    string TaxCategory) : IRequest<CatalogItemDto>;
