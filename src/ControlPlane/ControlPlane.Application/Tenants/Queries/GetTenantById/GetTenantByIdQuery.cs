using Api.Domain.Common;
using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ControlPlane.Application.Tenants.Queries.GetTenantById;

public record GetTenantByIdQuery(Guid TenantId) : IRequest<TenantDto>;

public class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator() => RuleFor(x => x.TenantId).NotEmpty();
}

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly ITenantRepository _repo;

    public GetTenantByIdQueryHandler(ITenantRepository repo) => _repo = repo;

    public async Task<TenantDto> Handle(GetTenantByIdQuery query, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(query.TenantId, ct)
            ?? throw new DomainException($"Tenant '{query.TenantId}' not found.");

        return tenant.ToDto();
    }
}
