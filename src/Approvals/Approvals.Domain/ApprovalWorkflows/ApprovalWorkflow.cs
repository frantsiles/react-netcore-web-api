using Api.Domain.Common;

namespace Approvals.Domain.ApprovalWorkflows;

public class ApprovalWorkflow : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string EntityType { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal? AmountThreshold { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<WorkflowApprover> _approvers = [];
    public IReadOnlyList<WorkflowApprover> Approvers => _approvers.AsReadOnly();
    public IReadOnlyList<Guid> ApproverUserIds => _approvers.Select(a => a.UserId).ToList().AsReadOnly();

    private ApprovalWorkflow() { }

    public static ApprovalWorkflow Create(
        Guid tenantId,
        string entityType,
        string name,
        IEnumerable<Guid> approverUserIds,
        decimal? amountThreshold = null)
    {
        var wf = new ApprovalWorkflow
        {
            TenantId = tenantId,
            EntityType = entityType,
            Name = name,
            AmountThreshold = amountThreshold,
            IsActive = true
        };
        foreach (var uid in approverUserIds)
            wf._approvers.Add(WorkflowApprover.Create(uid));
        return wf;
    }

    public void Update(string name, IEnumerable<Guid> approverUserIds, decimal? amountThreshold)
    {
        Name = name;
        AmountThreshold = amountThreshold;
        _approvers.Clear();
        foreach (var uid in approverUserIds)
            _approvers.Add(WorkflowApprover.Create(uid));
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("Workflow is already inactive.");
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        if (IsActive) throw new DomainException("Workflow is already active.");
        IsActive = true;
        SetUpdatedAt();
    }

    public bool RequiresApproval(decimal? amount) =>
        IsActive && (AmountThreshold == null || amount >= AmountThreshold);
}
