namespace Sales.Domain.Orders;

public enum SalesOrderStatus
{
    Draft,
    Confirmed,
    PartiallyFulfilled,
    Fulfilled,
    Invoiced,
    Cancelled
}
