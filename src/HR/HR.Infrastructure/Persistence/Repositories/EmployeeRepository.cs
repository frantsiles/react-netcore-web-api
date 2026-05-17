using HR.Domain.Employees;
using HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Persistence.Repositories;

public class EmployeeRepository(HrDbContext db) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Employee?> GetByNumberAsync(string employeeNumber, CancellationToken ct = default) =>
        await db.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber, ct);

    public async Task<List<Employee>> ListAsync(Guid? departmentId, EmployeeStatus? status, CancellationToken ct = default)
    {
        var q = db.Employees.AsQueryable();
        if (departmentId.HasValue) q = q.Where(e => e.DepartmentId == departmentId.Value);
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        return await q.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToListAsync(ct);
    }

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
    {
        await db.Employees.AddAsync(employee, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken ct = default)
    {
        db.Employees.Update(employee);
        await db.SaveChangesAsync(ct);
    }
}
