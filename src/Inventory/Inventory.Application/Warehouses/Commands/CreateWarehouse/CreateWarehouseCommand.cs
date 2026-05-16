using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.Warehouses.Commands.CreateWarehouse;

public record CreateWarehouseCommand(
    string Code,
    string Name,
    string? Address) : IRequest<WarehouseDto>;

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly IWarehouseRepository _repo;
    private readonly IEventPublisher _events;

    public CreateWarehouseCommandHandler(IWarehouseRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<WarehouseDto> Handle(CreateWarehouseCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsByCodeAsync(cmd.Code, ct))
            throw new DomainException($"Warehouse code '{cmd.Code.ToUpperInvariant()}' already exists.");

        var warehouse = Warehouse.Create(cmd.Code, cmd.Name, cmd.Address);
        await _repo.AddAsync(warehouse, ct);
        foreach (var e in warehouse.DomainEvents)
            await _events.PublishAsync(e, ct);

        return warehouse.ToDto();
    }
}
