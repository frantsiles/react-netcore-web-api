using Api.Domain.Common;
using HR.Application.Common.Dtos;
using HR.Application.Common.Mappings;
using HR.Domain.Repositories;
using MediatR;

namespace HR.Application.Employees.Queries.GetEmployeeById;

public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeDto>;

public class GetEmployeeByIdHandler(IEmployeeRepository repo)
    : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery query, CancellationToken ct)
    {
        var employee = await repo.GetByIdAsync(query.Id, ct)
            ?? throw new DomainException($"Employee {query.Id} not found.");
        return employee.ToDto();
    }
}
