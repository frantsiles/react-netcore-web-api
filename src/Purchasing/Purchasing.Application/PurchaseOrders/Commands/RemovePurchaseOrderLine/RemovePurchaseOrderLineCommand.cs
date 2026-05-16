using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Commands.RemovePurchaseOrderLine;

public record RemovePurchaseOrderLineCommand(Guid PurchaseOrderId, Guid LineId) : IRequest<PurchaseOrderDto>;

public class RemovePurchaseOrderLineCommandValidator : AbstractValidator<RemovePurchaseOrderLineCommand>
{
    public RemovePurchaseOrderLineCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}

public class RemovePurchaseOrderLineCommandHandler : IRequestHandler<RemovePurchaseOrderLineCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;

    public RemovePurchaseOrderLineCommandHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<PurchaseOrderDto> Handle(RemovePurchaseOrderLineCommand cmd, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(cmd.PurchaseOrderId, ct)
            ?? throw new DomainException($"Purchase order '{cmd.PurchaseOrderId}' not found.");

        po.RemoveLine(cmd.LineId);
        await _repo.UpdateAsync(po, ct);
        return po.ToDto();
    }
}
