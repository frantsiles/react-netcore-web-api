using FiscalCR.Application.DTOs;
using MediatR;

namespace FiscalCR.Application.Commands;

/// <summary>Generates, signs and submits a CR electronic invoice to Hacienda ATV.</summary>
public record TimbrarFacturaCommand(Guid InvoiceId) : IRequest<ElectronicDocumentDto>;

/// <summary>Polls Hacienda for the latest status of a submitted document.</summary>
public record PollDocumentStatusCommand(Guid InvoiceId) : IRequest<ElectronicDocumentDto>;

/// <summary>Returns the current FE status for an invoice (no external call).</summary>
public record GetElectronicDocumentQuery(Guid InvoiceId) : IRequest<ElectronicDocumentDto?>;
