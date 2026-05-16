using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Commands.CancelPurchaseOrder;

public record CancelPurchaseOrderCommand(Guid PurchaseOrderId, string Reason) : IRequest<PurchaseOrderDto>;

public class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CancelPurchaseOrderCommandHandler : IRequestHandler<CancelPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly IEventPublisher _events;

    public CancelPurchaseOrderCommandHandler(IPurchaseOrderRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<PurchaseOrderDto> Handle(CancelPurchaseOrderCommand cmd, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(cmd.PurchaseOrderId, ct)
            ?? throw new DomainException($"Purchase order '{cmd.PurchaseOrderId}' not found.");

        po.Cancel(cmd.Reason);
        await _repo.UpdateAsync(po, ct);
        foreach (var e in po.DomainEvents)
            await _events.PublishAsync(e, ct);

        return po.ToDto();
    }
}
