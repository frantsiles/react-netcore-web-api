using Approvals.Application.Common.Dtos;
using Approvals.Application.Common.Mappings;
using Approvals.Domain.ApprovalRequests;
using Approvals.Domain.Repositories;
using MediatR;

namespace Approvals.Application.ApprovalRequests.Queries.ListApprovalRequests;

public record ListApprovalRequestsQuery(
    string? EntityType,
    Guid? EntityId,
    ApprovalRequestStatus? Status) : IRequest<List<ApprovalRequestDto>>;

public class ListApprovalRequestsHandler(IApprovalRequestRepository repo)
    : IRequestHandler<ListApprovalRequestsQuery, List<ApprovalRequestDto>>
{
    public async Task<List<ApprovalRequestDto>> Handle(ListApprovalRequestsQuery query, CancellationToken ct)
    {
        if (query.Status == ApprovalRequestStatus.Pending && query.EntityId == null)
            return (await repo.ListPendingAsync(query.EntityType, ct)).Select(r => r.ToDto()).ToList();

        if (query.EntityType != null && query.EntityId.HasValue)
            return (await repo.ListByEntityAsync(query.EntityType, query.EntityId.Value, ct))
                .Where(r => query.Status == null || r.Status == query.Status)
                .Select(r => r.ToDto()).ToList();

        return (await repo.ListPendingAsync(query.EntityType, ct)).Select(r => r.ToDto()).ToList();
    }
}
