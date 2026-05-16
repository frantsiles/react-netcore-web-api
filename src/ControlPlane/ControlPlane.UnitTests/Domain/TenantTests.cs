using ControlPlane.Domain.Tenants;
using FluentAssertions;

namespace ControlPlane.UnitTests.Domain;

public class TenantTests
{
    [Fact]
    public void Create_ValidArgs_SetsProperties()
    {
        var tenant = Tenant.Create("Acme Corp", "acme", "US", "USD", TenantPlan.Standard);

        tenant.Name.Should().Be("Acme Corp");
        tenant.Slug.Should().Be("acme");
        tenant.CountryCode.Should().Be("US");
        tenant.CurrencyCode.Should().Be("USD");
        tenant.Plan.Should().Be(TenantPlan.Standard);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_NormalizesSlugToLowercase()
    {
        var tenant = Tenant.Create("Test", "MY-SLUG", "GB", "GBP");

        tenant.Slug.Should().Be("my-slug");
    }

    [Fact]
    public void Create_EmptyName_ThrowsDomainException()
    {
        var act = () => Tenant.Create("", "slug", "US", "USD");

        act.Should().Throw<Api.Domain.Common.DomainException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Create_InvalidCountryCode_ThrowsDomainException()
    {
        var act = () => Tenant.Create("Name", "slug", "USA", "USD");

        act.Should().Throw<Api.Domain.Common.DomainException>()
            .WithMessage("*CountryCode*");
    }

    [Fact]
    public void Suspend_ActiveTenant_Suspends()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");

        tenant.Suspend();

        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void Suspend_AlreadySuspended_ThrowsDomainException()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");
        tenant.Suspend();

        var act = () => tenant.Suspend();

        act.Should().Throw<Api.Domain.Common.DomainException>()
            .WithMessage("*already suspended*");
    }

    [Fact]
    public void Reactivate_SuspendedTenant_Activates()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");
        tenant.Suspend();

        tenant.Reactivate();

        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void SetSetting_NewKey_AddsSetting()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");

        tenant.SetSetting("max_users", "100");

        tenant.GetSetting("max_users").Should().Be("100");
        tenant.Settings.Should().HaveCount(1);
    }

    [Fact]
    public void SetSetting_ExistingKey_UpdatesValue()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");
        tenant.SetSetting("max_users", "50");

        tenant.SetSetting("max_users", "200");

        tenant.GetSetting("max_users").Should().Be("200");
        tenant.Settings.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveSetting_ExistingKey_RemovesSetting()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");
        tenant.SetSetting("max_users", "100");

        tenant.RemoveSetting("max_users");

        tenant.Settings.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSetting_NonExistingKey_ThrowsDomainException()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");

        var act = () => tenant.RemoveSetting("nonexistent");

        act.Should().Throw<Api.Domain.Common.DomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void GetSetting_NonExistingKey_ReturnsNull()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");

        var result = tenant.GetSetting("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void CreateWithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var tenant = Tenant.CreateWithId(id, "Acme", "acme", "US", "USD");

        tenant.Id.Should().Be(id);
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Create_RaisesTenantCreatedEvent()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");

        tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ControlPlane.Domain.DomainEvents.TenantCreatedEvent>();
    }
}
