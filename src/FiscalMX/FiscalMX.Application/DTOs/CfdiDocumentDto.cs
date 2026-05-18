namespace FiscalMX.Application.DTOs;

public record CfdiDocumentDto(
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

public record TenantFeMxConfig(
    string EmisorRfc,
    string EmisorNombre,
    string RegimenFiscal,
    string LugarExpedicion,
    string? PacApiUrl,
    string? PacToken);
