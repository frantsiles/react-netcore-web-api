using Api.Application.Common.Interfaces;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Contracts;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Contracts.Commands.CreateContract;

public record CreateContractCommand(
    Guid EmployeeId,
    string ContractNumber,
    DateOnly StartDate,
    decimal GrossSalary,
    string CurrencyCode,
    DateOnly? EndDate,
    string? Notes) : IRequest<ContractDto>;

public class CreateContractHandler(IContractRepository repo, ITenantContext tenant)
    : IRequestHandler<CreateContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(CreateContractCommand cmd, CancellationToken ct)
    {
        var contract = Contract.Create(
            tenant.TenantId, cmd.EmployeeId, cmd.ContractNumber,
            cmd.StartDate, cmd.GrossSalary, cmd.CurrencyCode, cmd.EndDate, cmd.Notes);
        await repo.AddAsync(contract, ct);
        return contract.ToDto();
    }
}
