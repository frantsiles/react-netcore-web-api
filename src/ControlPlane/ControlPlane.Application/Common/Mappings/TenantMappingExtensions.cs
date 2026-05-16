using ControlPlane.Application.Common.Dtos;
using ControlPlane.Domain.Tenants;

namespace ControlPlane.Application.Common.Mappings;

public static class TenantMappingExtensions
{
    public static TenantDto ToDto(this Tenant t) => new(
        t.Id, t.Name, t.Slug, t.CountryCode, t.CurrencyCode, t.Plan, t.Status,
        t.Settings.Select(s => new TenantSettingDto(s.Key, s.Value)).ToList().AsReadOnly(),
        t.CreatedAt, t.UpdatedAt);
}
