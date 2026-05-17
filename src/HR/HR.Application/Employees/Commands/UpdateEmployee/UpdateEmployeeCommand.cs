using Api.Domain.Common;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Employees.Commands.UpdateEmployee;

public record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string JobTitle,
    Guid DepartmentId,
    string? ManagerEmployeeId) : IRequest<EmployeeDto>;

public class UpdateEmployeeHandler(IEmployeeRepository repo)
    : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(UpdateEmployeeCommand cmd, CancellationToken ct)
    {
        var employee = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new DomainException($"Employee {cmd.Id} not found.");
        employee.UpdateProfile(cmd.FirstName, cmd.LastName, cmd.Email, cmd.Phone,
            cmd.JobTitle, cmd.DepartmentId, cmd.ManagerEmployeeId);
        await repo.UpdateAsync(employee, ct);
        return employee.ToDto();
    }
}
