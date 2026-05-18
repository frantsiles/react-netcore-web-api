namespace BFF.Application.FiscalCR;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ElectronicDocumentBffDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    string Clave,
    string NumeroConsecutivo,
    string Status,
    string? HaciendaEstado,
    string? HaciendaMensaje,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? AcknowledgedAt);

// ── Commands / Queries ────────────────────────────────────────────────────────

public record TimbrarFacturaBffCommand(string Token, Guid InvoiceId)
    : MediatR.IRequest<ElectronicDocumentBffDto>;

public record GetElectronicDocumentBffQuery(string Token, Guid InvoiceId)
    : MediatR.IRequest<ElectronicDocumentBffDto?>;

public record PollDocumentStatusBffCommand(string Token, Guid InvoiceId)
    : MediatR.IRequest<ElectronicDocumentBffDto>;
