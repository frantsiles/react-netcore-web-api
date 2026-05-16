using Api.Domain.Common;
using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ControlPlane.Application.Tenants.Commands.SuspendTenant;

public record SuspendTenantCommand(Guid TenantId) : IRequest<TenantDto>;

public class SuspendTenantCommandValidator : AbstractValidator<SuspendTenantCommand>
{
    public SuspendTenantCommandValidator() => RuleFor(x => x.TenantId).NotEmpty();
}

public class SuspendTenantCommandHandler : IRequestHandler<SuspendTenantCommand, TenantDto>
{
    private readonly ITenantRepository _repo;

    public SuspendTenantCommandHandler(ITenantRepository repo) => _repo = repo;

    public async Task<TenantDto> Handle(SuspendTenantCommand cmd, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(cmd.TenantId, ct)
            ?? throw new DomainException($"Tenant '{cmd.TenantId}' not found.");

        tenant.Suspend();
        await _repo.UpdateAsync(tenant, ct);
        return tenant.ToDto();
    }
}
