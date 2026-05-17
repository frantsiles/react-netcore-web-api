using Api.Domain.Common;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Employees.Commands.TerminateEmployee;

public record TerminateEmployeeCommand(Guid Id, DateOnly TerminationDate, string Reason) : IRequest<EmployeeDto>;

public class TerminateEmployeeHandler(IEmployeeRepository repo)
    : IRequestHandler<TerminateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(TerminateEmployeeCommand cmd, CancellationToken ct)
    {
        var employee = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new DomainException($"Employee {cmd.Id} not found.");
        employee.Terminate(cmd.TerminationDate, cmd.Reason);
        await repo.UpdateAsync(employee, ct);
        return employee.ToDto();
    }
}
