using Approvals.Domain.ApprovalRequests;

namespace Approvals.Domain.Repositories;

public interface IApprovalRequestRepository
{
    Task<ApprovalRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ApprovalRequest>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<List<ApprovalRequest>> ListPendingAsync(string? entityType, CancellationToken ct = default);
    Task AddAsync(ApprovalRequest request, CancellationToken ct = default);
    Task UpdateAsync(ApprovalRequest request, CancellationToken ct = default);
}
