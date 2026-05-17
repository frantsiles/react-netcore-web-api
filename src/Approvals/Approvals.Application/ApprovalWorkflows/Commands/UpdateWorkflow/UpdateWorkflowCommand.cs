using Api.Domain.Common;
using Approvals.Application.Common.Dtos;
using Approvals.Application.Common.Mappings;
using Approvals.Domain.Repositories;
using MediatR;

namespace Approvals.Application.ApprovalWorkflows.Commands.UpdateWorkflow;

public record UpdateWorkflowCommand(
    Guid Id,
    string Name,
    List<Guid> ApproverUserIds,
    decimal? AmountThreshold) : IRequest<ApprovalWorkflowDto>;

public class UpdateWorkflowHandler(IApprovalWorkflowRepository repo)
    : IRequestHandler<UpdateWorkflowCommand, ApprovalWorkflowDto>
{
    public async Task<ApprovalWorkflowDto> Handle(UpdateWorkflowCommand cmd, CancellationToken ct)
    {
        var workflow = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new DomainException($"Approval workflow {cmd.Id} not found.");
        workflow.Update(cmd.Name, cmd.ApproverUserIds, cmd.AmountThreshold);
        await repo.UpdateAsync(workflow, ct);
        return workflow.ToDto();
    }
}
