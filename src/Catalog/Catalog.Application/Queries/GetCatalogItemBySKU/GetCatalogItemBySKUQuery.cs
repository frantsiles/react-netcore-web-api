using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Queries.GetCatalogItemBySKU;

public record GetCatalogItemBySKUQuery(string SKU) : IRequest<CatalogItemDto>;
