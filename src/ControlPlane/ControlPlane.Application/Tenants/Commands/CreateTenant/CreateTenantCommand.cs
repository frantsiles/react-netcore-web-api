using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using ControlPlane.Application.Common.Dtos;
using ControlPlane.Application.Common.Mappings;
using ControlPlane.Domain.Repositories;
using ControlPlane.Domain.Tenants;
using FluentValidation;
using MediatR;

namespace ControlPlane.Application.Tenants.Commands.CreateTenant;

public record CreateTenantCommand(
    string Name,
    string Slug,
    string CountryCode,
    string CurrencyCode,
    TenantPlan Plan = TenantPlan.Free) : IRequest<TenantDto>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(60)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase alphanumeric with hyphens.");
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
    }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _repo;
    private readonly IEventPublisher _events;

    public CreateTenantCommandHandler(ITenantRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<TenantDto> Handle(CreateTenantCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsBySlugAsync(cmd.Slug, ct))
            throw new DomainException($"Tenant slug '{cmd.Slug}' is already taken.");

        var tenant = Tenant.Create(cmd.Name, cmd.Slug, cmd.CountryCode, cmd.CurrencyCode, cmd.Plan);
        await _repo.AddAsync(tenant, ct);
        foreach (var e in tenant.DomainEvents)
            await _events.PublishAsync(e, ct);

        return tenant.ToDto();
    }
}
