using Api.Application.Common.Interfaces;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Departments;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Departments.Commands.CreateDepartment;

public record CreateDepartmentCommand(
    string Code,
    string Name,
    Guid? ParentDepartmentId,
    string? CostCenter) : IRequest<DepartmentDto>;

public class CreateDepartmentHandler(IDepartmentRepository repo, ITenantContext tenant)
    : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = Department.Create(tenant.TenantId, cmd.Code, cmd.Name, cmd.ParentDepartmentId, cmd.CostCenter);
        await repo.AddAsync(dept, ct);
        return dept.ToDto();
    }
}
