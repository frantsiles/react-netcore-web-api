using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Queries.GetCatalogItemById;

public record GetCatalogItemByIdQuery(Guid CatalogItemId) : IRequest<CatalogItemDto>;
