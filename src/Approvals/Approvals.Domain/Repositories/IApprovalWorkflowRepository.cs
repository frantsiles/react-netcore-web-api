using Approvals.Domain.ApprovalWorkflows;

namespace Approvals.Domain.Repositories;

public interface IApprovalWorkflowRepository
{
    Task<ApprovalWorkflow?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApprovalWorkflow?> GetByEntityTypeAsync(string entityType, CancellationToken ct = default);
    Task<List<ApprovalWorkflow>> ListAsync(CancellationToken ct = default);
    Task AddAsync(ApprovalWorkflow workflow, CancellationToken ct = default);
    Task UpdateAsync(ApprovalWorkflow workflow, CancellationToken ct = default);
}
