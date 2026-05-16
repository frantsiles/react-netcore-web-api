using Api.Domain.Common;
using ControlPlane.Application.Common.Interfaces;
using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ControlPlane.Application.Tenants.Commands.RemoveTenantSetting;

public record RemoveTenantSettingCommand(Guid TenantId, string Key) : IRequest<TenantDto>;

public class RemoveTenantSettingCommandValidator : AbstractValidator<RemoveTenantSettingCommand>
{
    public RemoveTenantSettingCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty();
    }
}

public class RemoveTenantSettingCommandHandler
    : IRequestHandler<RemoveTenantSettingCommand, TenantDto>
{
    private readonly ITenantRepository _repo;
    private readonly ITenantSettingsCache _cache;

    public RemoveTenantSettingCommandHandler(ITenantRepository repo, ITenantSettingsCache cache)
        => (_repo, _cache) = (repo, cache);

    public async Task<TenantDto> Handle(RemoveTenantSettingCommand cmd, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(cmd.TenantId, ct)
            ?? throw new DomainException($"Tenant '{cmd.TenantId}' not found.");

        tenant.RemoveSetting(cmd.Key);
        await _repo.UpdateAsync(tenant, ct);
        _cache.Invalidate(cmd.TenantId);

        return tenant.ToDto();
    }
}
