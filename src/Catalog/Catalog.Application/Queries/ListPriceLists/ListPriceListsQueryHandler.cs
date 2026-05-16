using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Queries.ListPriceLists;

public class ListPriceListsQueryHandler(IPriceListRepository repository)
    : IRequestHandler<ListPriceListsQuery, IReadOnlyList<PriceListSummaryDto>>
{
    public async Task<IReadOnlyList<PriceListSummaryDto>> Handle(ListPriceListsQuery request, CancellationToken ct)
    {
        var lists = await repository.ListAllAsync(ct);
        return lists.Select(pl => pl.ToSummaryDto()).ToList();
    }
}
