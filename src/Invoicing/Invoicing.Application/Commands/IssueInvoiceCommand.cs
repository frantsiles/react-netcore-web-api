using Invoicing.Application.DTOs;
using MediatR;

namespace Invoicing.Application.Commands;

public record IssueInvoiceCommand(Guid InvoiceId) : IRequest<InvoiceDto>;
