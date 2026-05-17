using Api.Domain.Common;
using Approvals.Application.Common.Dtos;
using Approvals.Application.Common.Mappings;
using Approvals.Domain.Repositories;
using MediatR;

namespace Approvals.Application.ApprovalRequests.Commands.RejectRequest;

public record RejectRequestCommand(Guid RequestId, Guid ApproverUserId, string? Notes) : IRequest<ApprovalRequestDto>;

public class RejectRequestHandler(IApprovalRequestRepository repo)
    : IRequestHandler<RejectRequestCommand, ApprovalRequestDto>
{
    public async Task<ApprovalRequestDto> Handle(RejectRequestCommand cmd, CancellationToken ct)
    {
        var request = await repo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new DomainException($"Approval request {cmd.RequestId} not found.");
        request.Reject(cmd.ApproverUserId, cmd.Notes);
        await repo.UpdateAsync(request, ct);
        return request.ToDto();
    }
}
