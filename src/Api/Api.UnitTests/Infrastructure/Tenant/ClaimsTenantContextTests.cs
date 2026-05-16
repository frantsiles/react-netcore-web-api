using Api.Infrastructure.Tenant;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Api.UnitTests.Infrastructure.Tenant;

public class ClaimsTenantContextTests
{
    private static IHttpContextAccessor BuildAccessor(
        string? tenantId = null,
        string? countryCode = null)
    {
        var claims = new List<Claim>();
        if (tenantId is not null)    claims.Add(new Claim("tenant_id",    tenantId));
        if (countryCode is not null) claims.Add(new Claim("country_code", countryCode));

        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return accessor;
    }

    [Fact]
    public void TenantId_WhenClaimPresent_ShouldReturnParsedGuid()
    {
        var id = Guid.NewGuid();
        var sut = new ClaimsTenantContext(BuildAccessor(tenantId: id.ToString()));

        sut.TenantId.Should().Be(id);
    }

    [Fact]
    public void TenantId_WhenClaimAbsent_ShouldReturnDefaultTenantId()
    {
        var sut = new ClaimsTenantContext(BuildAccessor());

        sut.TenantId.Should().Be(ClaimsTenantContext.DefaultTenantId);
    }

    [Fact]
    public void TenantId_WhenNoHttpContext_ShouldReturnDefaultTenantId()
    {
        var accessor = new HttpContextAccessor(); // HttpContext is null
        var sut = new ClaimsTenantContext(accessor);

        sut.TenantId.Should().Be(ClaimsTenantContext.DefaultTenantId);
    }

    [Fact]
    public void CountryCode_WhenClaimPresent_ShouldReturnIt()
    {
        var sut = new ClaimsTenantContext(BuildAccessor(countryCode: "MX"));

        sut.CountryCode.Should().Be("MX");
    }

    [Fact]
    public void CountryCode_WhenClaimAbsent_ShouldReturnDefaultCountryCode()
    {
        var sut = new ClaimsTenantContext(BuildAccessor());

        sut.CountryCode.Should().Be(ClaimsTenantContext.DefaultCountryCode);
    }
}
