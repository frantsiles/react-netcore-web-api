using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Queries.SearchInventory;

public record SearchInventoryQuery(
    Guid? WarehouseId,
    string? SKU,
    bool? BelowReorderPoint,
    int Skip = 0,
    int Take = 50) : IRequest<IReadOnlyList<InventoryItemDto>>;

public class SearchInventoryQueryHandler
    : IRequestHandler<SearchInventoryQuery, IReadOnlyList<InventoryItemDto>>
{
    private readonly IInventoryItemRepository _repo;

    public SearchInventoryQueryHandler(IInventoryItemRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<InventoryItemDto>> Handle(
        SearchInventoryQuery query, CancellationToken ct)
    {
        var results = await _repo.SearchAsync(
            query.WarehouseId, query.SKU, query.BelowReorderPoint,
            query.Skip, query.Take, ct);
        return results.Select(i => i.ToDto()).ToList().AsReadOnly();
    }
}
