using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.ReleaseReservation;

public record ReleaseReservationCommand(
    Guid CatalogItemId,
    Guid WarehouseId,
    decimal Quantity,
    Guid SalesOrderId) : IRequest<InventoryItemDto>;

public class ReleaseReservationCommandValidator : AbstractValidator<ReleaseReservationCommand>
{
    public ReleaseReservationCommandValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.SalesOrderId).NotEmpty();
    }
}

public class ReleaseReservationCommandHandler
    : IRequestHandler<ReleaseReservationCommand, InventoryItemDto>
{
    private readonly IInventoryItemRepository _repo;

    public ReleaseReservationCommandHandler(IInventoryItemRepository repo) => _repo = repo;

    public async Task<InventoryItemDto> Handle(ReleaseReservationCommand cmd, CancellationToken ct)
    {
        var item = await _repo.GetByCatalogItemAndWarehouseAsync(cmd.CatalogItemId, cmd.WarehouseId, ct)
            ?? throw new DomainException(
                $"No inventory found for item '{cmd.CatalogItemId}' in warehouse '{cmd.WarehouseId}'.");

        item.ReleaseReservation(cmd.Quantity, cmd.SalesOrderId);
        await _repo.UpdateAsync(item, ct);
        return item.ToDto();
    }
}
