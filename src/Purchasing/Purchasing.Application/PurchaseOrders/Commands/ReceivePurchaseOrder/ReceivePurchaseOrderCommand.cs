using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Inventory.Application.Inventory.Commands.ReceiveStock;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Commands.ReceivePurchaseOrder;

public record ReceivePurchaseOrderCommand(
    Guid PurchaseOrderId,
    Guid LineId,
    Guid WarehouseId,
    decimal QuantityReceived) : IRequest<PurchaseOrderDto>;

public class ReceivePurchaseOrderCommandValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.QuantityReceived).GreaterThan(0);
    }
}

public class ReceivePurchaseOrderCommandHandler : IRequestHandler<ReceivePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ISender _sender;
    private readonly IEventPublisher _events;

    public ReceivePurchaseOrderCommandHandler(
        IPurchaseOrderRepository repo, ISender sender, IEventPublisher events)
        => (_repo, _sender, _events) = (repo, sender, events);

    public async Task<PurchaseOrderDto> Handle(ReceivePurchaseOrderCommand cmd, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(cmd.PurchaseOrderId, ct)
            ?? throw new DomainException($"Purchase order '{cmd.PurchaseOrderId}' not found.");

        var line = po.Lines.FirstOrDefault(l => l.Id == cmd.LineId)
            ?? throw new DomainException($"Line '{cmd.LineId}' not found.");

        po.ReceiveLine(cmd.LineId, cmd.QuantityReceived);
        await _repo.UpdateAsync(po, ct);

        // Stock into Inventory module
        await _sender.Send(new ReceiveStockCommand(
            line.CatalogItemId,
            cmd.WarehouseId,
            line.Sku,
            cmd.QuantityReceived,
            ReferenceNumber: po.PoNumber,
            Notes: $"Received via PO {po.PoNumber}",
            ReorderPoint: null), ct);

        foreach (var e in po.DomainEvents)
            await _events.PublishAsync(e, ct);

        return po.ToDto();
    }
}
