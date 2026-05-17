using Invoicing.Application.DTOs;
using MediatR;

namespace Invoicing.Application.Commands;

public record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string CurrencyCode,
    DateOnly PaidAt,
    string? Reference) : IRequest<InvoiceDto>;
