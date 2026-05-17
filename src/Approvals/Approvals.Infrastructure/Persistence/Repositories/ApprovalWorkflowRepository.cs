using Approvals.Domain.ApprovalWorkflows;
using Approvals.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Approvals.Infrastructure.Persistence.Repositories;

public class ApprovalWorkflowRepository(ApprovalsDbContext db) : IApprovalWorkflowRepository
{
    public async Task<ApprovalWorkflow?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.ApprovalWorkflows.Include("_approvers").FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<ApprovalWorkflow?> GetByEntityTypeAsync(string entityType, CancellationToken ct = default) =>
        await db.ApprovalWorkflows.Include("_approvers")
            .FirstOrDefaultAsync(w => w.EntityType == entityType && w.IsActive, ct);

    public async Task<List<ApprovalWorkflow>> ListAsync(CancellationToken ct = default) =>
        await db.ApprovalWorkflows.Include("_approvers").OrderBy(w => w.EntityType).ToListAsync(ct);

    public async Task AddAsync(ApprovalWorkflow workflow, CancellationToken ct = default)
    {
        await db.ApprovalWorkflows.AddAsync(workflow, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ApprovalWorkflow workflow, CancellationToken ct = default)
    {
        db.ApprovalWorkflows.Update(workflow);
        await db.SaveChangesAsync(ct);
    }
}
