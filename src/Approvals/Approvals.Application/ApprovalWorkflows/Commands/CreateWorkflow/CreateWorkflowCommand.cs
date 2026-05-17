using Api.Application.Common.Interfaces;
using Approvals.Application.Common.Dtos;
using Approvals.Application.Common.Mappings;
using Approvals.Domain.ApprovalWorkflows;
using Approvals.Domain.Repositories;
using MediatR;

namespace Approvals.Application.ApprovalWorkflows.Commands.CreateWorkflow;

public record CreateWorkflowCommand(
    string EntityType,
    string Name,
    List<Guid> ApproverUserIds,
    decimal? AmountThreshold) : IRequest<ApprovalWorkflowDto>;

public class CreateWorkflowHandler(IApprovalWorkflowRepository repo, ITenantContext tenant)
    : IRequestHandler<CreateWorkflowCommand, ApprovalWorkflowDto>
{
    public async Task<ApprovalWorkflowDto> Handle(CreateWorkflowCommand cmd, CancellationToken ct)
    {
        var workflow = ApprovalWorkflow.Create(
            tenant.TenantId, cmd.EntityType, cmd.Name, cmd.ApproverUserIds, cmd.AmountThreshold);
        await repo.AddAsync(workflow, ct);
        return workflow.ToDto();
    }
}
