using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Employees;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Employees.Commands.HireEmployee;

public record HireEmployeeCommand(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId,
    string JobTitle,
    EmploymentType EmploymentType,
    DateOnly HireDate,
    string? Phone,
    string? ManagerEmployeeId) : IRequest<EmployeeDto>;

public class HireEmployeeHandler(IEmployeeRepository repo, ITenantContext tenant)
    : IRequestHandler<HireEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(HireEmployeeCommand cmd, CancellationToken ct)
    {
        var existing = await repo.GetByNumberAsync(cmd.EmployeeNumber, ct);
        if (existing != null)
            throw new DomainException($"Employee number '{cmd.EmployeeNumber}' already exists.");

        var employee = Employee.Hire(
            tenant.TenantId, cmd.EmployeeNumber, cmd.FirstName, cmd.LastName,
            cmd.Email, cmd.DepartmentId, cmd.JobTitle, cmd.EmploymentType,
            cmd.HireDate, cmd.Phone, cmd.ManagerEmployeeId);

        await repo.AddAsync(employee, ct);
        return employee.ToDto();
    }
}
