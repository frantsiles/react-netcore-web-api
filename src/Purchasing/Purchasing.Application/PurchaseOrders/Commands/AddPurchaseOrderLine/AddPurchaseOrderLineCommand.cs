using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Commands.AddPurchaseOrderLine;

public record AddPurchaseOrderLineCommand(
    Guid PurchaseOrderId,
    Guid CatalogItemId,
    string Sku,
    string ItemName,
    decimal QuantityOrdered,
    decimal UnitCostAmount,
    string? Notes = null) : IRequest<PurchaseOrderDto>;

public class AddPurchaseOrderLineCommandValidator : AbstractValidator<AddPurchaseOrderLineCommand>
{
    public AddPurchaseOrderLineCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.QuantityOrdered).GreaterThan(0);
        RuleFor(x => x.UnitCostAmount).GreaterThanOrEqualTo(0);
    }
}

public class AddPurchaseOrderLineCommandHandler : IRequestHandler<AddPurchaseOrderLineCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;

    public AddPurchaseOrderLineCommandHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<PurchaseOrderDto> Handle(AddPurchaseOrderLineCommand cmd, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(cmd.PurchaseOrderId, ct)
            ?? throw new DomainException($"Purchase order '{cmd.PurchaseOrderId}' not found.");

        po.AddLine(cmd.CatalogItemId, cmd.Sku, cmd.ItemName, cmd.QuantityOrdered,
            Money.Of(cmd.UnitCostAmount, po.CurrencyCode), cmd.Notes);

        await _repo.UpdateAsync(po, ct);
        return po.ToDto();
    }
}
