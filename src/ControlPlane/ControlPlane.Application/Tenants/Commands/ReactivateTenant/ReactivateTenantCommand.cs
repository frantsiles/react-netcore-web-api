using Api.Domain.Common;
using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ControlPlane.Application.Tenants.Commands.ReactivateTenant;

public record ReactivateTenantCommand(Guid TenantId) : IRequest<TenantDto>;

public class ReactivateTenantCommandValidator : AbstractValidator<ReactivateTenantCommand>
{
    public ReactivateTenantCommandValidator() => RuleFor(x => x.TenantId).NotEmpty();
}

public class ReactivateTenantCommandHandler : IRequestHandler<ReactivateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _repo;

    public ReactivateTenantCommandHandler(ITenantRepository repo) => _repo = repo;

    public async Task<TenantDto> Handle(ReactivateTenantCommand cmd, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(cmd.TenantId, ct)
            ?? throw new DomainException($"Tenant '{cmd.TenantId}' not found.");

        tenant.Reactivate();
        await _repo.UpdateAsync(tenant, ct);
        return tenant.ToDto();
    }
}
