using Api.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Api.Infrastructure.Tenant;

/// <summary>
/// Resolves tenant context from JWT claims (tenant_id + country_code).
/// Falls back to the default tenant during E0–E3 (single-tenant mode).
/// Replaced by a real multi-tenant resolver in E3.5.
/// </summary>
public sealed class ClaimsTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    // Well-known GUID for the single default tenant used in E0–E3.
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string DefaultCountryCode = "US";

    public Guid TenantId
    {
        get
        {
            var claim = accessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : DefaultTenantId;
        }
    }

    public string CountryCode =>
        accessor.HttpContext?.User.FindFirst("country_code")?.Value ?? DefaultCountryCode;
}
