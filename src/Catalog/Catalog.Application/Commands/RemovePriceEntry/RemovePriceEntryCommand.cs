using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Commands.RemovePriceEntry;

public record RemovePriceEntryCommand(Guid PriceListId, Guid EntryId) : IRequest<PriceListDto>;
