using System;

namespace Invoicing.Application.Commands;

public record ConvertSalesOrderToInvoiceCommand(Guid SalesOrderId, Guid? InvoiceId = null);
