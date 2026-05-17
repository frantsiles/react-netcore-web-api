using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Departments.Queries.ListDepartments;

public record ListDepartmentsQuery(bool? IsActive) : IRequest<List<DepartmentDto>>;

public class ListDepartmentsHandler(IDepartmentRepository repo)
    : IRequestHandler<ListDepartmentsQuery, List<DepartmentDto>>
{
    public async Task<List<DepartmentDto>> Handle(ListDepartmentsQuery query, CancellationToken ct)
    {
        var depts = await repo.ListAsync(query.IsActive, ct);
        return depts.Select(d => d.ToDto()).ToList();
    }
}
