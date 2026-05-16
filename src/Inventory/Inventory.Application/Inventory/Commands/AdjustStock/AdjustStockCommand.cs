using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid InventoryItemId,
    decimal Delta,
    string Reason,
    string? Notes) : IRequest<InventoryItemDto>;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Delta cannot be zero.");
        RuleFor(x => x.Reason).NotEmpty();
    }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, InventoryItemDto>
{
    private readonly IInventoryItemRepository _repo;
    private readonly IEventPublisher _events;

    public AdjustStockCommandHandler(IInventoryItemRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<InventoryItemDto> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(cmd.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{cmd.InventoryItemId}' not found.");

        item.Adjust(cmd.Delta, cmd.Reason, cmd.Notes);
        await _repo.UpdateAsync(item, ct);
        foreach (var e in item.DomainEvents)
            await _events.PublishAsync(e, ct);

        return item.ToDto();
    }
}
