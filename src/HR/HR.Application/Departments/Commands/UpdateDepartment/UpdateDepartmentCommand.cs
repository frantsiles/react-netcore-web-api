using Api.Domain.Common;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Departments.Commands.UpdateDepartment;

public record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    Guid? ParentDepartmentId,
    string? CostCenter) : IRequest<DepartmentDto>;

public class UpdateDepartmentHandler(IDepartmentRepository repo)
    : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new DomainException($"Department {cmd.Id} not found.");
        dept.Update(cmd.Name, cmd.ParentDepartmentId, cmd.CostCenter);
        await repo.UpdateAsync(dept, ct);
        return dept.ToDto();
    }
}
