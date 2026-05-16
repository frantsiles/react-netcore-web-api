using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.ReserveStock;

public record ReserveStockCommand(
    Guid CatalogItemId,
    Guid WarehouseId,
    decimal Quantity,
    Guid SalesOrderId,
    string? OrderNumber) : IRequest<InventoryItemDto>;

public class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockCommandValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.SalesOrderId).NotEmpty();
    }
}

public class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, InventoryItemDto>
{
    private readonly IInventoryItemRepository _repo;
    private readonly IEventPublisher _events;

    public ReserveStockCommandHandler(IInventoryItemRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<InventoryItemDto> Handle(ReserveStockCommand cmd, CancellationToken ct)
    {
        var item = await _repo.GetByCatalogItemAndWarehouseAsync(cmd.CatalogItemId, cmd.WarehouseId, ct)
            ?? throw new DomainException(
                $"No inventory found for item '{cmd.CatalogItemId}' in warehouse '{cmd.WarehouseId}'.");

        item.Reserve(cmd.Quantity, cmd.SalesOrderId, cmd.OrderNumber);
        await _repo.UpdateAsync(item, ct);
        foreach (var e in item.DomainEvents)
            await _events.PublishAsync(e, ct);

        return item.ToDto();
    }
}
