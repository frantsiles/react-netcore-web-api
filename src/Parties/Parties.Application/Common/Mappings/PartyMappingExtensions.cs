using Parties.Application.Common.Dtos;
using Parties.Domain.Parties;

namespace Parties.Application.Common.Mappings;

public static class PartyMappingExtensions
{
    public static PartyDto ToDto(this Party party) => new(
        party.Id,
        party.LegalName,
        party.TradeName,
        party.PartyType.ToString(),
        party.CountryCode,
        party.TaxId?.Value,
        party.IsActive,
        party.Roles.Select(r => new PartyRoleDto(
            r.RoleType.ToString(),
            r.Status.ToString(),
            r.CreditLimit?.Amount,
            r.CreditLimit?.CurrencyCode,
            r.PaymentTermsDays,
            r.EmployeeNumber)).ToList(),
        party.Addresses.Select(a => new AddressDto(
            a.Line1, a.Line2, a.City, a.StateOrRegion,
            a.PostalCode, a.CountryCode, a.IsPrimary)).ToList(),
        party.ContactPoints.Select(c => new ContactPointDto(
            c.Type.ToString(), c.Value, c.IsPrimary)).ToList());
}
