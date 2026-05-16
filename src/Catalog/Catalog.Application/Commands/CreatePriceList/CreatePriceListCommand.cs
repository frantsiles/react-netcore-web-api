using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Commands.CreatePriceList;

public record CreatePriceListCommand(
    string Name,
    string CurrencyCode,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsDefault) : IRequest<PriceListDto>;
