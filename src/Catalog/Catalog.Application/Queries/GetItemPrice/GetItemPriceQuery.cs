using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Queries.GetItemPrice;

public record GetItemPriceQuery(
    Guid CatalogItemId,
    decimal Quantity,
    DateOnly? OnDate = null) : IRequest<ItemPriceDto?>;
