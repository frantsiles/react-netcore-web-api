using Api.Domain.Common;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Queries.GetInventoryItem;

public record GetInventoryItemQuery(Guid InventoryItemId) : IRequest<InventoryItemDto>;

public class GetInventoryItemQueryHandler
    : IRequestHandler<GetInventoryItemQuery, InventoryItemDto>
{
    private readonly IInventoryItemRepository _repo;

    public GetInventoryItemQueryHandler(IInventoryItemRepository repo) => _repo = repo;

    public async Task<InventoryItemDto> Handle(GetInventoryItemQuery query, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(query.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{query.InventoryItemId}' not found.");
        return item.ToDto();
    }
}
