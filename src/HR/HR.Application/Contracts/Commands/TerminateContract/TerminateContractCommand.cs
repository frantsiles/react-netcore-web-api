using Api.Domain.Common;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Contracts.Commands.TerminateContract;

public record TerminateContractCommand(Guid Id, string? Notes) : IRequest<ContractDto>;

public class TerminateContractHandler(IContractRepository repo)
    : IRequestHandler<TerminateContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(TerminateContractCommand cmd, CancellationToken ct)
    {
        var contract = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new DomainException($"Contract {cmd.Id} not found.");
        contract.Terminate(cmd.Notes);
        await repo.UpdateAsync(contract, ct);
        return contract.ToDto();
    }
}
