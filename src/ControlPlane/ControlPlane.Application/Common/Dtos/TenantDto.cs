using ControlPlane.Domain.Tenants;

namespace ControlPlane.Application.Common.Dtos;

public record TenantSettingDto(string Key, string Value);

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string CountryCode,
    string CurrencyCode,
    TenantPlan Plan,
    TenantStatus Status,
    IReadOnlyList<TenantSettingDto> Settings,
    DateTime CreatedAt,
    DateTime UpdatedAt);
