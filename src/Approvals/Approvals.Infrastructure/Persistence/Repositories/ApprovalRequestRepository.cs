using Approvals.Domain.ApprovalRequests;
using Approvals.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Approvals.Infrastructure.Persistence.Repositories;

public class ApprovalRequestRepository(ApprovalsDbContext db) : IApprovalRequestRepository
{
    public async Task<ApprovalRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<ApprovalRequest>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default) =>
        await db.ApprovalRequests
            .Where(r => r.EntityType == entityType && r.EntityId == entityId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<ApprovalRequest>> ListPendingAsync(string? entityType, CancellationToken ct = default)
    {
        var q = db.ApprovalRequests.Where(r => r.Status == ApprovalRequestStatus.Pending);
        if (entityType != null) q = q.Where(r => r.EntityType == entityType);
        return await q.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(ApprovalRequest request, CancellationToken ct = default)
    {
        await db.ApprovalRequests.AddAsync(request, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ApprovalRequest request, CancellationToken ct = default)
    {
        db.ApprovalRequests.Update(request);
        await db.SaveChangesAsync(ct);
    }
}
