using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Employees;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Employees.Queries.ListEmployees;

public record ListEmployeesQuery(Guid? DepartmentId, EmployeeStatus? Status) : IRequest<List<EmployeeDto>>;

public class ListEmployeesHandler(IEmployeeRepository repo)
    : IRequestHandler<ListEmployeesQuery, List<EmployeeDto>>
{
    public async Task<List<EmployeeDto>> Handle(ListEmployeesQuery query, CancellationToken ct)
    {
        var employees = await repo.ListAsync(query.DepartmentId, query.Status, ct);
        return employees.Select(e => e.ToDto()).ToList();
    }
}
