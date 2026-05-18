namespace BFF.Application.FiscalMX;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record CfdiDocumentBffDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    string? Uuid,
    string? Serie,
    string? Folio,
    string Status,
    string? PacMensaje,
    string? CancelMotivo,
    DateTime CreatedAt,
    DateTime? TimbradoAt,
    DateTime? CancelledAt);

// ── Commands / Queries ────────────────────────────────────────────────────────

public record TimbrarCfdiBffCommand(string Token, Guid InvoiceId)
    : MediatR.IRequest<CfdiDocumentBffDto>;

public record GetCfdiDocumentBffQuery(string Token, Guid InvoiceId)
    : MediatR.IRequest<CfdiDocumentBffDto?>;

public record CancelarCfdiBffCommand(string Token, Guid InvoiceId, string Motivo, string? UuidRelacionado)
    : MediatR.IRequest<CfdiDocumentBffDto>;
