using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Commands.UpdatePurchaseOrderLine;

public record UpdatePurchaseOrderLineCommand(
    Guid PurchaseOrderId,
    Guid LineId,
    decimal QuantityOrdered,
    decimal UnitCostAmount,
    string? Notes = null) : IRequest<PurchaseOrderDto>;

public class UpdatePurchaseOrderLineCommandValidator : AbstractValidator<UpdatePurchaseOrderLineCommand>
{
    public UpdatePurchaseOrderLineCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.QuantityOrdered).GreaterThan(0);
        RuleFor(x => x.UnitCostAmount).GreaterThanOrEqualTo(0);
    }
}

public class UpdatePurchaseOrderLineCommandHandler : IRequestHandler<UpdatePurchaseOrderLineCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;

    public UpdatePurchaseOrderLineCommandHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<PurchaseOrderDto> Handle(UpdatePurchaseOrderLineCommand cmd, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(cmd.PurchaseOrderId, ct)
            ?? throw new DomainException($"Purchase order '{cmd.PurchaseOrderId}' not found.");

        po.UpdateLine(cmd.LineId, cmd.QuantityOrdered,
            Money.Of(cmd.UnitCostAmount, po.CurrencyCode), cmd.Notes);

        await _repo.UpdateAsync(po, ct);
        return po.ToDto();
    }
}
