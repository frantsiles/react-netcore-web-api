using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using ControlPlane.Domain.Tenants;
using MediatR;

namespace ControlPlane.Application.Tenants.Queries.ListTenants;

public record ListTenantsQuery(
    string? NameFilter = null,
    TenantStatus? Status = null,
    TenantPlan? Plan = null) : IRequest<IReadOnlyList<TenantDto>>;

public class ListTenantsQueryHandler : IRequestHandler<ListTenantsQuery, IReadOnlyList<TenantDto>>
{
    private readonly ITenantRepository _repo;

    public ListTenantsQueryHandler(ITenantRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TenantDto>> Handle(ListTenantsQuery query, CancellationToken ct)
    {
        var tenants = await _repo.ListAsync(query.NameFilter, query.Status, query.Plan, ct);
        return tenants.Select(t => t.ToDto()).ToList();
    }
}
