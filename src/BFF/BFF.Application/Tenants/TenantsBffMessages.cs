namespace BFF.Application.Tenants;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record TenantSettingBffDto(string Key, string Value);

public record TenantBffDto(
    Guid Id, string Name, string Slug, string CountryCode, string CurrencyCode,
    string Plan, string Status,
    IReadOnlyList<TenantSettingBffDto> Settings,
    DateTime CreatedAt, DateTime UpdatedAt);

// ── Queries ───────────────────────────────────────────────────────────────────

public record ListTenantsBffQuery(string Token, string? Name, string? Status, string? Plan)
    : MediatR.IRequest<IReadOnlyList<TenantBffDto>>;

public record GetTenantByIdBffQuery(string Token, Guid TenantId)
    : MediatR.IRequest<TenantBffDto?>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateTenantBffCommand(
    string Token, string Name, string Slug, string CountryCode, string CurrencyCode, string? Plan)
    : MediatR.IRequest<TenantBffDto>;

public record UpdateTenantBffCommand(
    string Token, Guid TenantId, string Name, string CountryCode, string CurrencyCode, string Plan)
    : MediatR.IRequest<TenantBffDto>;

public record SuspendTenantBffCommand(string Token, Guid TenantId)
    : MediatR.IRequest<TenantBffDto>;

public record ReactivateTenantBffCommand(string Token, Guid TenantId)
    : MediatR.IRequest<TenantBffDto>;

public record SetTenantSettingBffCommand(string Token, Guid TenantId, string Key, string Value)
    : MediatR.IRequest<TenantBffDto>;

public record RemoveTenantSettingBffCommand(string Token, Guid TenantId, string Key)
    : MediatR.IRequest<TenantBffDto>;
