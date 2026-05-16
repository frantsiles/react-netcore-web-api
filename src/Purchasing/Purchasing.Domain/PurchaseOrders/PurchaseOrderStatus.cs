namespace Purchasing.Domain.PurchaseOrders;

public enum PurchaseOrderStatus
{
    Draft,
    Sent,
    Confirmed,
    PartiallyReceived,
    Received,
    Cancelled
}
