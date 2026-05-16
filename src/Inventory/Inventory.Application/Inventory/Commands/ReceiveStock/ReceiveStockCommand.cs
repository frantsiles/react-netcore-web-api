using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Inventory;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.ReceiveStock;

public record ReceiveStockCommand(
    Guid CatalogItemId,
    Guid WarehouseId,
    string SKU,
    decimal Quantity,
    string? ReferenceNumber,
    string? Notes,
    decimal? ReorderPoint) : IRequest<InventoryItemDto>;

public class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockCommandValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class ReceiveStockCommandHandler : IRequestHandler<ReceiveStockCommand, InventoryItemDto>
{
    private readonly IInventoryItemRepository _repo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IEventPublisher _events;

    public ReceiveStockCommandHandler(IInventoryItemRepository repo,
        IWarehouseRepository warehouseRepo, IEventPublisher events)
        => (_repo, _warehouseRepo, _events) = (repo, warehouseRepo, events);

    public async Task<InventoryItemDto> Handle(ReceiveStockCommand cmd, CancellationToken ct)
    {
        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.WarehouseId, ct)
            ?? throw new DomainException($"Warehouse '{cmd.WarehouseId}' not found.");

        var item = await _repo.GetByCatalogItemAndWarehouseAsync(cmd.CatalogItemId, cmd.WarehouseId, ct);

        if (item is null)
        {
            item = InventoryItem.Create(cmd.CatalogItemId, cmd.WarehouseId, cmd.SKU, cmd.ReorderPoint);
            item.Receive(cmd.Quantity, cmd.ReferenceNumber, cmd.Notes);
            await _repo.AddAsync(item, ct);
        }
        else
        {
            item.Receive(cmd.Quantity, cmd.ReferenceNumber, cmd.Notes);
            if (cmd.ReorderPoint.HasValue)
                item.UpdateReorderPoint(cmd.ReorderPoint);
            await _repo.UpdateAsync(item, ct);
        }

        foreach (var e in item.DomainEvents)
            await _events.PublishAsync(e, ct);

        return item.ToDto();
    }
}
