using Api.Domain.Common;
using FluentAssertions;
using Parties.Domain.Parties;
using Parties.Domain.ValueObjects;

namespace Parties.UnitTests.Domain;

public class PartyTests
{
    private static PartyRole ACustomerRole() => PartyRole.Create(PartyRoleType.Customer);
    private static Address AnAddress() =>
        Address.Create("123 Main St", null, "Springfield", "IL", "62701", "US");

    [Fact]
    public void Create_WithValidData_CreatesActiveParty()
    {
        var party = Party.Create("Acme Corp", PartyType.Organization, "US", ACustomerRole());

        party.LegalName.Should().Be("Acme Corp");
        party.PartyType.Should().Be(PartyType.Organization);
        party.CountryCode.Should().Be("US");
        party.IsActive.Should().BeTrue();
        party.Roles.Should().HaveCount(1);
        party.Roles[0].RoleType.Should().Be(PartyRoleType.Customer);
        party.Roles[0].IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithEmptyLegalName_ThrowsDomainException(string name)
    {
        var act = () => Party.Create(name, PartyType.Organization, "US", ACustomerRole());

        act.Should().Throw<DomainException>().WithMessage("*Legal name*");
    }

    [Fact]
    public void Create_WithNullRole_ThrowsDomainException()
    {
        var act = () => Party.Create("Acme", PartyType.Organization, "US", null!);

        act.Should().Throw<DomainException>().WithMessage("*role*");
    }

    [Fact]
    public void Create_RaisesPartyRegisteredEvent()
    {
        var party = Party.Create("Acme Corp", PartyType.Organization, "US", ACustomerRole());

        party.DomainEvents.Should().HaveCount(1);
        party.DomainEvents[0].Should().BeOfType<Parties.Domain.DomainEvents.PartyRegisteredEvent>();
    }

    [Fact]
    public void AddAddress_FirstAddress_IsMarkedAsPrimary()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());

        party.AddAddress(AnAddress());

        party.Addresses.Should().HaveCount(1);
        party.Addresses[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void AddAddress_SecondAddress_IsNotPrimary()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());
        party.AddAddress(AnAddress());
        party.AddAddress(AnAddress());

        party.Addresses[1].IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void RemoveAddress_OnlyAddress_ThrowsDomainException()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());
        party.AddAddress(AnAddress());

        var act = () => party.RemoveAddress(0);

        act.Should().Throw<DomainException>().WithMessage("*at least one address*");
    }

    [Fact]
    public void RemoveAddress_RemovesPrimary_PromotesNext()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());
        party.AddAddress(AnAddress());
        party.AddAddress(AnAddress());

        party.RemoveAddress(0);

        party.Addresses.Should().HaveCount(1);
        party.Addresses[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void ActivateRole_WhenRoleAlreadyActive_IsIdempotent()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());
        party.ClearDomainEvents();

        party.ActivateRole(PartyRoleType.Customer);

        party.Roles.Should().HaveCount(1);
        party.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ActivateRole_NewRole_AddsRoleAndRaisesEvent()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());
        party.ClearDomainEvents();

        party.ActivateRole(PartyRoleType.Supplier);

        party.Roles.Should().HaveCount(2);
        party.DomainEvents.Should().HaveCount(1);
        party.DomainEvents[0].Should().BeOfType<Parties.Domain.DomainEvents.PartyRoleActivatedEvent>();
    }

    [Fact]
    public void DeactivateRole_LastActiveRole_ThrowsDomainException()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());

        var act = () => party.DeactivateRole(PartyRoleType.Customer);

        act.Should().Throw<DomainException>().WithMessage("*last active role*");
    }

    [Fact]
    public void DeactivateRole_WithTwoActiveRoles_Succeeds()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());
        party.ActivateRole(PartyRoleType.Supplier);
        party.ClearDomainEvents();

        party.DeactivateRole(PartyRoleType.Supplier);

        party.Roles.First(r => r.RoleType == PartyRoleType.Supplier).IsActive.Should().BeFalse();
        party.DomainEvents.Should().HaveCount(1);
        party.DomainEvents[0].Should().BeOfType<Parties.Domain.DomainEvents.PartyRoleDeactivatedEvent>();
    }

    [Fact]
    public void UpdateProfile_RaisesPartyProfileUpdatedEvent()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());
        party.ClearDomainEvents();

        party.UpdateProfile("Acme Corp Ltd", "Acme", null);

        party.LegalName.Should().Be("Acme Corp Ltd");
        party.TradeName.Should().Be("Acme");
        party.DomainEvents.Should().HaveCount(1);
        party.DomainEvents[0].Should().BeOfType<Parties.Domain.DomainEvents.PartyProfileUpdatedEvent>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US", ACustomerRole());

        party.Deactivate();

        party.IsActive.Should().BeFalse();
    }
}
