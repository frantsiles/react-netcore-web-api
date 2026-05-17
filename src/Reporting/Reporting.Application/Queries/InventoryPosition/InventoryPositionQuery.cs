using MediatR;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Application.Queries.InventoryPosition;

public record InventoryPositionQuery : IRequest<InventoryPositionReportDto>;

public class InventoryPositionHandler(IReportingStore store)
    : IRequestHandler<InventoryPositionQuery, InventoryPositionReportDto>
{
    public Task<InventoryPositionReportDto> Handle(InventoryPositionQuery query, CancellationToken ct) =>
        store.GetInventoryPositionAsync(ct);
}
