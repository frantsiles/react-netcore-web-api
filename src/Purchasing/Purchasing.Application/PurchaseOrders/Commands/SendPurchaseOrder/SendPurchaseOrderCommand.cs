using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Commands.SendPurchaseOrder;

public record SendPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest<PurchaseOrderDto>;

public class SendPurchaseOrderCommandValidator : AbstractValidator<SendPurchaseOrderCommand>
{
    public SendPurchaseOrderCommandValidator() => RuleFor(x => x.PurchaseOrderId).NotEmpty();
}

public class SendPurchaseOrderCommandHandler : IRequestHandler<SendPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly IEventPublisher _events;

    public SendPurchaseOrderCommandHandler(IPurchaseOrderRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<PurchaseOrderDto> Handle(SendPurchaseOrderCommand cmd, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(cmd.PurchaseOrderId, ct)
            ?? throw new DomainException($"Purchase order '{cmd.PurchaseOrderId}' not found.");

        po.Send();
        await _repo.UpdateAsync(po, ct);
        foreach (var e in po.DomainEvents)
            await _events.PublishAsync(e, ct);

        return po.ToDto();
    }
}
