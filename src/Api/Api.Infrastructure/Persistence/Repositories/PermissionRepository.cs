using Api.Domain.Permissions;
using Api.Domain.Permissions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Repositories;

public class PermissionRepository(AppDbContext context) : IPermissionRepository
{
    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Permission?> GetByNameAsync(string name, CancellationToken ct = default)
        => await context.Permissions.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default)
        => await context.Permissions.OrderBy(p => p.Name).ToListAsync(ct);

    public async Task AddAsync(Permission permission, CancellationToken ct = default)
    {
        await context.Permissions.AddAsync(permission, ct);
        await context.SaveChangesAsync(ct);
    }
}
