using MediatR;

namespace Catalog.Application.Commands.SetDefaultPriceList;

public record SetDefaultPriceListCommand(Guid PriceListId) : IRequest;
