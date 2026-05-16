using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Commands.UpdatePriceList;

public record UpdatePriceListCommand(
    Guid PriceListId,
    string Name,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : IRequest<PriceListDto>;
