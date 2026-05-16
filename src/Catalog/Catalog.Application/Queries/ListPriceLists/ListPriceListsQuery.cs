using Catalog.Application.Common.Dtos;
using MediatR;

namespace Catalog.Application.Queries.ListPriceLists;

public record ListPriceListsQuery : IRequest<IReadOnlyList<PriceListSummaryDto>>;
