namespace FiscalCR.Application.DTOs;

public record ElectronicDocumentDto(
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
