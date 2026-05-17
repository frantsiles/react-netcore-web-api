using Api.Domain.Common;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Contracts.Queries.ListContracts;

public record ListContractsQuery(Guid EmployeeId) : IRequest<List<ContractDto>>;

public class ListContractsHandler(IContractRepository repo)
    : IRequestHandler<ListContractsQuery, List<ContractDto>>
{
    public async Task<List<ContractDto>> Handle(ListContractsQuery query, CancellationToken ct)
    {
        var contracts = await repo.ListByEmployeeAsync(query.EmployeeId, ct);
        return contracts.Select(c => c.ToDto()).ToList();
    }
}
