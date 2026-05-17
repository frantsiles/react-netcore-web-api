using Api.Domain.Common;
using Approvals.Application.Common.Dtos;
using Approvals.Application.Common.Mappings;
using Approvals.Domain.Repositories;
using MediatR;

namespace Approvals.Application.ApprovalRequests.Queries.GetApprovalRequest;

public record GetApprovalRequestQuery(Guid Id) : IRequest<ApprovalRequestDto>;

public class GetApprovalRequestHandler(IApprovalRequestRepository repo)
    : IRequestHandler<GetApprovalRequestQuery, ApprovalRequestDto>
{
    public async Task<ApprovalRequestDto> Handle(GetApprovalRequestQuery query, CancellationToken ct)
    {
        var request = await repo.GetByIdAsync(query.Id, ct)
            ?? throw new DomainException($"Approval request {query.Id} not found.");
        return request.ToDto();
    }
}
