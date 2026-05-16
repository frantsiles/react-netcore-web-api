using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Commands.AddPriceEntry;

public record AddPriceEntryCommand(
    Guid PriceListId,
    Guid CatalogItemId,
    decimal UnitPrice,
    decimal MinQuantity = 1m) : IRequest<PriceListDto>;
