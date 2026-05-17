using System;
using Invoicing.Application.Commands;

namespace Invoicing.Application.Handlers;

public class ConvertSalesOrderToInvoiceHandler
{
    public object Handle(ConvertSalesOrderToInvoiceCommand command)
    {
        // TODO: implementar lógica: cargar SalesOrder, crear Invoice, reservar stock, calcular impuestos, persistir
        throw new NotImplementedException("ConvertSalesOrderToInvoiceHandler not implemented");
    }
}
