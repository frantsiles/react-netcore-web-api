using Api.Domain.Common;
using Inventory.Application.Common.Dtos;
using Inventory.Application.Common.Mappings;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Warehouses.Queries.GetWarehouse;

public record GetWarehouseQuery(Guid WarehouseId) : IRequest<WarehouseDto>;

public class GetWarehouseQueryHandler : IRequestHandler<GetWarehouseQuery, WarehouseDto>
{
    private readonly IWarehouseRepository _repo;

    public GetWarehouseQueryHandler(IWarehouseRepository repo) => _repo = repo;

    public async Task<WarehouseDto> Handle(GetWarehouseQuery query, CancellationToken ct)
    {
        var warehouse = await _repo.GetByIdAsync(query.WarehouseId, ct)
            ?? throw new DomainException($"Warehouse '{query.WarehouseId}' not found.");
        return warehouse.ToDto();
    }
}
