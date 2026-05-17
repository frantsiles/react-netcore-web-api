using Api.Domain.Common;

namespace Approvals.Domain.ApprovalWorkflows;

public class WorkflowApprover : Entity
{
    public Guid UserId { get; private set; }

    private WorkflowApprover() { }

    public static WorkflowApprover Create(Guid userId) => new() { UserId = userId };
}
