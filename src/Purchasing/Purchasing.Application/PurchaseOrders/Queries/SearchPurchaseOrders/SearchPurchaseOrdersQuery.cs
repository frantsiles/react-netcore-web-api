using Api.Application.Common.Interfaces;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.PurchaseOrders;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Queries.SearchPurchaseOrders;

public record SearchPurchaseOrdersQuery(
    Guid? SupplierId = null,
    PurchaseOrderStatus? Status = null,
    string? PoNumberFilter = null) : IRequest<IReadOnlyList<PurchaseOrderDto>>;

public class SearchPurchaseOrdersQueryHandler
    : IRequestHandler<SearchPurchaseOrdersQuery, IReadOnlyList<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ITenantContext _tenant;

    public SearchPurchaseOrdersQueryHandler(IPurchaseOrderRepository repo, ITenantContext tenant)
        => (_repo, _tenant) = (repo, tenant);

    public async Task<IReadOnlyList<PurchaseOrderDto>> Handle(
        SearchPurchaseOrdersQuery query, CancellationToken ct)
    {
        var results = await _repo.SearchAsync(
            _tenant.TenantId, query.SupplierId, query.Status, query.PoNumberFilter, ct);

        return results.Select(po => po.ToDto()).ToList();
    }
}
