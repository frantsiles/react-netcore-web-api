using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Queries.GetPriceListById;

public record GetPriceListByIdQuery(Guid PriceListId) : IRequest<PriceListDto>;
