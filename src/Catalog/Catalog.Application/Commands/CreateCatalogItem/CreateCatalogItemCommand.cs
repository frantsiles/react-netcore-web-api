using Catalog.Application.Common.Dtos;
using Catalog.Domain.Catalog;
using MediatR;

namespace Catalog.Application.Commands.CreateCatalogItem;

public record CreateCatalogItemCommand(
    string SKU,
    string Name,
    string? Description,
    ItemType ItemType,
    string UnitOfMeasure,
    string TaxCategory,
    string DefaultCurrency,
    string CountryCode,
    bool TrackInventory = false,
    decimal? ReorderPoint = null) : IRequest<CatalogItemDto>;
