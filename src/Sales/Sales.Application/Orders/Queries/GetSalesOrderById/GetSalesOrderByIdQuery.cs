using Api.Domain.Common;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Queries.GetSalesOrderById;

public record GetSalesOrderByIdQuery(Guid OrderId) : IRequest<SalesOrderDto>;

public class GetSalesOrderByIdQueryHandler : IRequestHandler<GetSalesOrderByIdQuery, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repo;

    public GetSalesOrderByIdQueryHandler(ISalesOrderRepository repo) => _repo = repo;

    public async Task<SalesOrderDto> Handle(GetSalesOrderByIdQuery query, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(query.OrderId, ct)
            ?? throw new DomainException($"SalesOrder '{query.OrderId}' not found.");
        return order.ToDto();
    }
}
