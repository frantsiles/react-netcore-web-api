using HR.Domain.Departments;

namespace HR.Domain.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Department>> ListAsync(bool? isActive, CancellationToken ct = default);
    Task AddAsync(Department department, CancellationToken ct = default);
    Task UpdateAsync(Department department, CancellationToken ct = default);
}
