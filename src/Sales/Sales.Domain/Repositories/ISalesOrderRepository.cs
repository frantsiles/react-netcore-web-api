using Sales.Domain.Orders;

namespace Sales.Domain.Repositories;

public interface ISalesOrderRepository
{
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<IReadOnlyList<SalesOrder>> SearchAsync(Guid? customerId, SalesOrderStatus? status,
        Guid? originQuoteId, int skip, int take, CancellationToken ct = default);
    Task AddAsync(SalesOrder order, CancellationToken ct = default);
    Task UpdateAsync(SalesOrder order, CancellationToken ct = default);
}
