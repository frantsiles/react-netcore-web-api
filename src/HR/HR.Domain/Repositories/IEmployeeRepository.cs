using HR.Domain.Employees;

namespace HR.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByNumberAsync(string employeeNumber, CancellationToken ct = default);
    Task<List<Employee>> ListAsync(Guid? departmentId, EmployeeStatus? status, CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
    Task UpdateAsync(Employee employee, CancellationToken ct = default);
}
