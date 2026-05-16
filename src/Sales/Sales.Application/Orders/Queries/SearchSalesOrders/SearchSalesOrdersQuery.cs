using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Orders;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Queries.SearchSalesOrders;

public record SearchSalesOrdersQuery(
    Guid? CustomerId,
    SalesOrderStatus? Status,
    Guid? OriginQuoteId,
    int Skip = 0,
    int Take = 20) : IRequest<IReadOnlyList<SalesOrderDto>>;

public class SearchSalesOrdersQueryHandler
    : IRequestHandler<SearchSalesOrdersQuery, IReadOnlyList<SalesOrderDto>>
{
    private readonly ISalesOrderRepository _repo;

    public SearchSalesOrdersQueryHandler(ISalesOrderRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SalesOrderDto>> Handle(
        SearchSalesOrdersQuery query, CancellationToken ct)
    {
        var results = await _repo.SearchAsync(
            query.CustomerId, query.Status, query.OriginQuoteId,
            query.Skip, query.Take, ct);
        return results.Select(o => o.ToDto()).ToList().AsReadOnly();
    }
}
