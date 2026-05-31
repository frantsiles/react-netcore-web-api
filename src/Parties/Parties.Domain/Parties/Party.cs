using Api.Domain.Common;
using Parties.Domain.DomainEvents;
using Parties.Domain.ValueObjects;

namespace Parties.Domain.Parties;

public class Party : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string CountryCode { get; private set; } = "";
    public PartyType PartyType { get; private set; }
    public string LegalName { get; private set; } = "";
    public string? TradeName { get; private set; }
    public TaxId? TaxId { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Address> _addresses = [];
    private readonly List<ContactPoint> _contactPoints = [];
    private readonly List<PartyRole> _roles = [];

    public IReadOnlyList<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyList<ContactPoint> ContactPoints => _contactPoints.AsReadOnly();
    public IReadOnlyList<PartyRole> Roles => _roles.AsReadOnly();

    private Party() : base() { }

    private Party(string legalName, string? tradeName, PartyType partyType,
        string countryCode, TaxId? taxId, PartyRole firstRole) : base()
    {
        LegalName = legalName;
        TradeName = tradeName;
        PartyType = partyType;
        CountryCode = countryCode.ToUpperInvariant();
        TaxId = taxId;
        IsActive = true;
        _roles.Add(firstRole);
    }

    public static Party Create(string legalName, PartyType partyType, string countryCode,
        PartyRole firstRole, string? tradeName = null, TaxId? taxId = null)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new DomainException("Legal name cannot be empty.");
        if (firstRole is null)
            throw new DomainException("A party must have at least one role.");
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new DomainException("Country code must be a 2-letter ISO code.");

        var party = new Party(legalName.Trim(), tradeName?.Trim(), partyType,
            countryCode, taxId, firstRole);

        party.RaiseDomainEvent(new PartyRegisteredEvent(party.Id, party.LegalName, DateTime.UtcNow));
        return party;
    }

    public void UpdateProfile(string legalName, string? tradeName, TaxId? taxId)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new DomainException("Legal name cannot be empty.");

        LegalName = legalName.Trim();
        TradeName = tradeName?.Trim();
        TaxId = taxId;
        SetUpdatedAt();
        RaiseDomainEvent(new PartyProfileUpdatedEvent(Id, LegalName, DateTime.UtcNow));
    }

    public void AddAddress(Address address)
    {
        if (_addresses.Count == 0)
            _addresses.Add(address.AsPrimary());
        else
            _addresses.Add(address.AsSecondary());
        SetUpdatedAt();
    }

    public void RemoveAddress(int index)
    {
        if (_addresses.Count <= 1)
            throw new DomainException("A party must have at least one address.");
        if (index < 0 || index >= _addresses.Count)
            throw new DomainException("Address index is out of range.");

        _addresses.RemoveAt(index);

        // If primary was removed, promote the first remaining
        if (!_addresses.Any(a => a.IsPrimary))
            _addresses[0] = _addresses[0].AsPrimary();

        SetUpdatedAt();
    }

    public void AddContactPoint(ContactPoint point)
    {
        _contactPoints.Add(point);
        SetUpdatedAt();
    }

    public void RemoveContactPoint(int index)
    {
        if (index < 0 || index >= _contactPoints.Count)
            throw new DomainException("Contact point index is out of range.");
        _contactPoints.RemoveAt(index);
        SetUpdatedAt();
    }

    public void ActivateRole(PartyRoleType roleType)
    {
        var role = _roles.FirstOrDefault(r => r.RoleType == roleType);
        if (role is null)
        {
            _roles.Add(PartyRole.Create(roleType));
            RaiseDomainEvent(new PartyRoleActivatedEvent(Id, roleType, DateTime.UtcNow));
        }
        else if (!role.IsActive)
        {
            role.Activate();
            RaiseDomainEvent(new PartyRoleActivatedEvent(Id, roleType, DateTime.UtcNow));
        }
        SetUpdatedAt();
    }

    public void DeactivateRole(PartyRoleType roleType)
    {
        var role = _roles.FirstOrDefault(r => r.RoleType == roleType)
            ?? throw new DomainException($"Role '{roleType}' not found on this party.");

        var activeCount = _roles.Count(r => r.IsActive);
        if (activeCount <= 1)
            throw new DomainException("Cannot deactivate the last active role.");

        role.Deactivate();
        RaiseDomainEvent(new PartyRoleDeactivatedEvent(Id, roleType, DateTime.UtcNow));
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Reactivate()
    {
        IsActive = true;
        SetUpdatedAt();
    }
}
