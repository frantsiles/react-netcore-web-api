namespace Parties.Application.Common.Dtos;

public record PartyDto(
    Guid PartyId,
    string LegalName,
    string? TradeName,
    string PartyType,
    string CountryCode,
    string? TaxId,
    bool IsActive,
    IReadOnlyList<PartyRoleDto> Roles,
    IReadOnlyList<AddressDto> Addresses,
    IReadOnlyList<ContactPointDto> ContactPoints);

public record PartyRoleDto(
    string RoleType,
    string Status,
    decimal? CreditLimit,
    string? CreditLimitCurrency,
    int? PaymentTermsDays,
    string? EmployeeNumber);

public record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string StateOrRegion,
    string PostalCode,
    string CountryCode,
    bool IsPrimary);

public record ContactPointDto(
    string Type,
    string Value,
    bool IsPrimary);
