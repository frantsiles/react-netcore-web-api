using Api.Domain.Common;
using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using ControlPlane.Domain.Tenants;
using FluentValidation;
using MediatR;

namespace ControlPlane.Application.Tenants.Commands.UpdateTenant;

public record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string CountryCode,
    string CurrencyCode,
    TenantPlan Plan) : IRequest<TenantDto>;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
    }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _repo;

    public UpdateTenantCommandHandler(ITenantRepository repo) => _repo = repo;

    public async Task<TenantDto> Handle(UpdateTenantCommand cmd, CancellationToken ct)
    {
        var tenant = await _repo.GetByIdAsync(cmd.TenantId, ct)
            ?? throw new DomainException($"Tenant '{cmd.TenantId}' not found.");

        tenant.Update(cmd.Name, cmd.CountryCode, cmd.CurrencyCode, cmd.Plan);
        await _repo.UpdateAsync(tenant, ct);
        return tenant.ToDto();
    }
}
