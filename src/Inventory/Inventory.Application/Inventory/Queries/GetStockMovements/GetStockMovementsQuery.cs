using Api.Domain.Common;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Inventory;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Queries.GetStockMovements;

public record GetStockMovementsQuery(
    Guid InventoryItemId,
    MovementType? Type,
    int Skip = 0,
    int Take = 100) : IRequest<IReadOnlyList<StockMovementDto>>;

public class GetStockMovementsQueryHandler
    : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    private readonly IInventoryItemRepository _repo;

    public GetStockMovementsQueryHandler(IInventoryItemRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<StockMovementDto>> Handle(
        GetStockMovementsQuery query, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(query.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{query.InventoryItemId}' not found.");

        var movements = item.Movements.AsEnumerable();
        if (query.Type.HasValue)
            movements = movements.Where(m => m.Type == query.Type.Value);

        return movements
            .OrderByDescending(m => m.OccurredAt)
            .Skip(query.Skip).Take(query.Take)
            .Select(m => m.ToDto())
            .ToList().AsReadOnly();
    }
}
