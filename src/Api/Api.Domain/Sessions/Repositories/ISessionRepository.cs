namespace Api.Domain.Sessions.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Session?> GetByRefreshTokenHashAsync(string hash, CancellationToken ct = default);
    Task<IReadOnlyList<Session>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Session>> GetAllActiveSessionsAsync(CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
    Task UpdateAsync(Session session, CancellationToken ct = default);
}
