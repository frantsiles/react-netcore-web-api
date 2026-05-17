using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Approvals;

public class ListApprovalRequestsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListApprovalRequestsBffQuery, IReadOnlyList<ApprovalRequestBffDto>>
{
    public async Task<IReadOnlyList<ApprovalRequestBffDto>> Handle(
        ListApprovalRequestsBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.EntityType)) parts.Add($"entityType={Uri.EscapeDataString(request.EntityType)}");
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"status={request.Status}");

        var url = parts.Count > 0
            ? $"api/approvals/requests?{string.Join("&", parts)}"
            : "api/approvals/requests";
        var result = await apiClient.GetAsync<List<ApprovalRequestBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class ApproveRequestBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ApproveRequestBffCommand, ApprovalRequestBffDto>
{
    public async Task<ApprovalRequestBffDto> Handle(
        ApproveRequestBffCommand request, CancellationToken ct)
    {
        var body = new { ApproverUserId = request.ApproverUserId, Notes = request.Notes };
        var result = await apiClient.PostAsync<object, ApprovalRequestBffDto>(
            $"api/approvals/requests/{request.RequestId}/approve", body, request.Token, ct);
        return result!;
    }
}

public class RejectRequestBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RejectRequestBffCommand, ApprovalRequestBffDto>
{
    public async Task<ApprovalRequestBffDto> Handle(
        RejectRequestBffCommand request, CancellationToken ct)
    {
        var body = new { ApproverUserId = request.ApproverUserId, Notes = request.Notes };
        var result = await apiClient.PostAsync<object, ApprovalRequestBffDto>(
            $"api/approvals/requests/{request.RequestId}/reject", body, request.Token, ct);
        return result!;
    }
}
