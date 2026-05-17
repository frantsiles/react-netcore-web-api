using Api.Domain.Common;
using Approvals.Application.Common.Dtos;
using Approvals.Application.Common.Mappings;
using Approvals.Domain.Repositories;
using MediatR;

namespace Approvals.Application.ApprovalRequests.Commands.ApproveRequest;

public record ApproveRequestCommand(Guid RequestId, Guid ApproverUserId, string? Notes) : IRequest<ApprovalRequestDto>;

public class ApproveRequestHandler(IApprovalRequestRepository repo)
    : IRequestHandler<ApproveRequestCommand, ApprovalRequestDto>
{
    public async Task<ApprovalRequestDto> Handle(ApproveRequestCommand cmd, CancellationToken ct)
    {
        var request = await repo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new DomainException($"Approval request {cmd.RequestId} not found.");
        request.Approve(cmd.ApproverUserId, cmd.Notes);
        await repo.UpdateAsync(request, ct);
        return request.ToDto();
    }
}
