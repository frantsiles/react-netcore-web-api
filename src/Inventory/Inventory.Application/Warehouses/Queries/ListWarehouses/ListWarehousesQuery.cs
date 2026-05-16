using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Warehouses.Queries.ListWarehouses;

public record ListWarehousesQuery : IRequest<IReadOnlyList<WarehouseDto>>;

public class ListWarehousesQueryHandler
    : IRequestHandler<ListWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IWarehouseRepository _repo;

    public ListWarehousesQueryHandler(IWarehouseRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<WarehouseDto>> Handle(
        ListWarehousesQuery query, CancellationToken ct)
    {
        var warehouses = await _repo.ListAllAsync(ct);
        return warehouses.Select(w => w.ToDto()).ToList().AsReadOnly();
    }
}
