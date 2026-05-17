using HR.Domain.Departments;
using HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Persistence.Repositories;

public class DepartmentRepository(HrDbContext db) : IDepartmentRepository
{
    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<List<Department>> ListAsync(bool? isActive, CancellationToken ct = default)
    {
        var q = db.Departments.AsQueryable();
        if (isActive.HasValue) q = q.Where(d => d.IsActive == isActive.Value);
        return await q.OrderBy(d => d.Code).ToListAsync(ct);
    }

    public async Task AddAsync(Department department, CancellationToken ct = default)
    {
        await db.Departments.AddAsync(department, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Department department, CancellationToken ct = default)
    {
        db.Departments.Update(department);
        await db.SaveChangesAsync(ct);
    }
}
