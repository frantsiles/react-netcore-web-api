using Invoicing.Application.DTOs;
using MediatR;

namespace Invoicing.Application.Commands;

public record CancelInvoiceCommand(Guid InvoiceId, string Reason) : IRequest<InvoiceDto>;
