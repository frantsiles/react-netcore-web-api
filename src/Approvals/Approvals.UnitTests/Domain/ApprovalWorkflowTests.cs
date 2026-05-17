using Api.Domain.Common;
using Approvals.Domain.ApprovalRequests;
using Approvals.Domain.ApprovalWorkflows;
using Approvals.Domain.DomainEvents;
using FluentAssertions;

namespace Approvals.UnitTests.Domain;

public class ApprovalWorkflowTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    private static ApprovalWorkflow NewWorkflow(decimal? threshold = null) =>
        ApprovalWorkflow.Create(TenantId, "PurchaseOrder", "PO Approval", [ApproverId], threshold);

    [Fact]
    public void Create_SetsActiveWithApprovers()
    {
        var wf = NewWorkflow(1000m);

        wf.EntityType.Should().Be("PurchaseOrder");
        wf.IsActive.Should().BeTrue();
        wf.ApproverUserIds.Should().ContainSingle().Which.Should().Be(ApproverId);
        wf.AmountThreshold.Should().Be(1000m);
    }

    [Fact]
    public void RequiresApproval_AboveThreshold_ReturnsTrue()
    {
        var wf = NewWorkflow(1000m);

        wf.RequiresApproval(1500m).Should().BeTrue();
    }

    [Fact]
    public void RequiresApproval_BelowThreshold_ReturnsFalse()
    {
        var wf = NewWorkflow(1000m);

        wf.RequiresApproval(500m).Should().BeFalse();
    }

    [Fact]
    public void RequiresApproval_NoThreshold_AlwaysTrue()
    {
        var wf = NewWorkflow(null);

        wf.RequiresApproval(0m).Should().BeTrue();
    }

    [Fact]
    public void RequiresApproval_InactiveWorkflow_ReturnsFalse()
    {
        var wf = NewWorkflow(null);
        wf.Deactivate();

        wf.RequiresApproval(9999m).Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsDomainException()
    {
        var wf = NewWorkflow();
        wf.Deactivate();

        var act = () => wf.Deactivate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_ReplacesApproversAndThreshold()
    {
        var wf = NewWorkflow(1000m);
        var newApprover = Guid.NewGuid();

        wf.Update("Updated", [newApprover], 2000m);

        wf.Name.Should().Be("Updated");
        wf.ApproverUserIds.Should().ContainSingle().Which.Should().Be(newApprover);
        wf.AmountThreshold.Should().Be(2000m);
    }
}

public class ApprovalRequestTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid WorkflowId = Guid.NewGuid();
    private static readonly Guid RequesterId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();
    private static readonly Guid EntityId = Guid.NewGuid();

    private static ApprovalRequest NewRequest() =>
        ApprovalRequest.Create(TenantId, WorkflowId, "PurchaseOrder", EntityId, "PO-001", RequesterId, 1500m);

    [Fact]
    public void Create_SetsPendingStatus()
    {
        var req = NewRequest();

        req.Status.Should().Be(ApprovalRequestStatus.Pending);
        req.EntityReference.Should().Be("PO-001");
    }

    [Fact]
    public void Create_RaisesApprovalRequestCreatedEvent()
    {
        var req = NewRequest();

        req.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ApprovalRequestCreatedEvent>();
    }

    [Fact]
    public void Approve_Pending_SetsApproved()
    {
        var req = NewRequest();

        req.Approve(ApproverId, "Looks good");

        req.Status.Should().Be(ApprovalRequestStatus.Approved);
        req.DecidedByUserId.Should().Be(ApproverId);
        req.DecisionNotes.Should().Be("Looks good");
        req.DecidedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_RaisesDecidedEvent()
    {
        var req = NewRequest();
        req.ClearDomainEvents();

        req.Approve(ApproverId);

        req.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ApprovalRequestDecidedEvent>()
            .Which.Approved.Should().BeTrue();
    }

    [Fact]
    public void Reject_Pending_SetsRejected()
    {
        var req = NewRequest();

        req.Reject(ApproverId, "Too expensive");

        req.Status.Should().Be(ApprovalRequestStatus.Rejected);
    }

    [Fact]
    public void Approve_AlreadyApproved_ThrowsDomainException()
    {
        var req = NewRequest();
        req.Approve(ApproverId);

        var act = () => req.Approve(ApproverId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_AlreadyRejected_ThrowsDomainException()
    {
        var req = NewRequest();
        req.Reject(ApproverId);

        var act = () => req.Reject(ApproverId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_Pending_SetsCancelled()
    {
        var req = NewRequest();

        req.Cancel();

        req.Status.Should().Be(ApprovalRequestStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Approved_ThrowsDomainException()
    {
        var req = NewRequest();
        req.Approve(ApproverId);

        var act = () => req.Cancel();

        act.Should().Throw<DomainException>();
    }
}
