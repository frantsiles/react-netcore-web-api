using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using ControlPlane.Application.Common.Interfaces;
using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ControlPlane.Application.Tenants.Commands.SetTenantSetting;

public record SetTenantSettingCommand(
    Guid TenantId,
    string Key,
    string Value) : IRequest<TenantDto>;

public class SetTenantSettingCommandValidator : AbstractValidator<SetTenantSettingCommand>
{
    public SetTenantSettingCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Value).NotNull();
    }
}

public class SetTenantSettingCommandHandler : IRequestHandler<SetTenantSettingCommand, TenantDto>
{
    private readonly ITenantRepository _repo;
    private readonly ITenantSettingsCache _cache;

    public SetTenantSettingCommandHandler(ITenantRepository repo, ITenantSettingsCache cache)
        => (_repo, _cache) = (repo, cache);

    public async Task<TenantDto> Handle(SetTenantSettingCommand cmd, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(cmd.TenantId, ct)
            ?? throw new DomainException($"Tenant '{cmd.TenantId}' not found.");

        tenant.SetSetting(cmd.Key, cmd.Value);
        await _repo.UpdateAsync(tenant, ct);
        _cache.Invalidate(cmd.TenantId);

        return tenant.ToDto();
    }
}
