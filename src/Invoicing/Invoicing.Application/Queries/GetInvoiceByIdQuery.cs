using Invoicing.Application.DTOs;
using MediatR;

namespace Invoicing.Application.Queries;

public record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<InvoiceDto?>;
