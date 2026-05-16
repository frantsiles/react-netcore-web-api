using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Queries.GetPurchaseOrderById;

public record GetPurchaseOrderByIdQuery(Guid PurchaseOrderId) : IRequest<PurchaseOrderDto>;

public class GetPurchaseOrderByIdQueryValidator : AbstractValidator<GetPurchaseOrderByIdQuery>
{
    public GetPurchaseOrderByIdQueryValidator() => RuleFor(x => x.PurchaseOrderId).NotEmpty();
}

public class GetPurchaseOrderByIdQueryHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;

    public GetPurchaseOrderByIdQueryHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<PurchaseOrderDto> Handle(GetPurchaseOrderByIdQuery query, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(query.PurchaseOrderId, ct)
            ?? throw new DomainException($"Purchase order '{query.PurchaseOrderId}' not found.");

        return po.ToDto();
    }
}
