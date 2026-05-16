using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Commands.UpdatePriceEntry;

public record UpdatePriceEntryCommand(
    Guid PriceListId,
    Guid EntryId,
    decimal UnitPrice,
    decimal MinQuantity) : IRequest<PriceListDto>;
