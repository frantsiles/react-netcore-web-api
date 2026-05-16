using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.WriteOffStock;

public record WriteOffStockCommand(
    Guid InventoryItemId,
    decimal Quantity,
    string Reason,
    string? Notes) : IRequest<InventoryItemDto>;

public class WriteOffStockCommandValidator : AbstractValidator<WriteOffStockCommand>
{
    public WriteOffStockCommandValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty();
    }
}

public class WriteOffStockCommandHandler : IRequestHandler<WriteOffStockCommand, InventoryItemDto>
{
    private readonly IInventoryItemRepository _repo;
    private readonly IEventPublisher _events;

    public WriteOffStockCommandHandler(IInventoryItemRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<InventoryItemDto> Handle(WriteOffStockCommand cmd, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(cmd.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{cmd.InventoryItemId}' not found.");

        item.WriteOff(cmd.Quantity, cmd.Reason, cmd.Notes);
        await _repo.UpdateAsync(item, ct);
        foreach (var e in item.DomainEvents)
            await _events.PublishAsync(e, ct);

        return item.ToDto();
    }
}
