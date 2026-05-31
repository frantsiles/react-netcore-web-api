using MediatR;

namespace BFF.Application.Parties;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record PartyBffDto(
    Guid PartyId,
    string LegalName,
    string? TradeName,
    string PartyType,
    string CountryCode,
    string? TaxId,
    bool IsActive,
    IReadOnlyList<PartyRoleBffDto> Roles,
    IReadOnlyList<AddressBffDto> Addresses,
    IReadOnlyList<ContactPointBffDto> ContactPoints);

public record PartyRoleBffDto(
    string RoleType,
    string Status,
    decimal? CreditLimit,
    string? CreditLimitCurrency,
    int? PaymentTermsDays,
    string? EmployeeNumber);

public record AddressBffDto(
    string Line1,
    string? Line2,
    string City,
    string StateOrRegion,
    string PostalCode,
    string CountryCode,
    bool IsPrimary);

public record ContactPointBffDto(
    string Type,
    string Value,
    bool IsPrimary);

// ── Queries ───────────────────────────────────────────────────────────────────

public record SearchPartiesBffQuery(
    string BearerToken,
    string? LegalName,
    string? RoleType,
    bool? IsActive,
    int Skip,
    int Take) : IRequest<IReadOnlyList<PartyBffDto>>;

public record GetPartyByIdBffQuery(
    string BearerToken,
    Guid PartyId) : IRequest<PartyBffDto?>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record RegisterPartyBffCommand(
    string BearerToken,
    string LegalName,
    string? TradeName,
    string PartyType,       // "Individual" | "Organization"
    string CountryCode,
    string FirstRoleType,   // "Customer" | "Supplier" | "Employee" | "Contact"
    string? TaxId,
    decimal? CreditLimit,
    string? CreditLimitCurrency,
    int? PaymentTermsDays,
    string? EmployeeNumber) : IRequest<PartyBffDto>;

public record UpdatePartyProfileBffCommand(
    string BearerToken,
    Guid PartyId,
    string LegalName,
    string? TradeName,
    string? TaxId) : IRequest<PartyBffDto>;

public record DeactivatePartyBffCommand(
    string BearerToken,
    Guid PartyId) : IRequest<Unit>;

public record ReactivatePartyBffCommand(
    string BearerToken,
    Guid PartyId) : IRequest<Unit>;
