using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Repositories;

public class SessionRepository(AppDbContext context) : ISessionRepository
{
    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Sessions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Session?> GetByRefreshTokenHashAsync(string hash, CancellationToken ct = default)
        => await context.Sessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);

    public async Task<IReadOnlyList<Session>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Session>> GetAllActiveSessionsAsync(CancellationToken ct = default)
        => await context.Sessions
            .Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        await context.Sessions.AddAsync(session, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Session session, CancellationToken ct = default)
    {
        context.Sessions.Update(session);
        await context.SaveChangesAsync(ct);
    }
}
