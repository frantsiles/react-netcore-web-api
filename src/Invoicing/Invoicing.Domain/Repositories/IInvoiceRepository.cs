using Invoicing.Domain.Invoices;

namespace Invoicing.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> SearchAsync(
        Guid? customerId,
        InvoiceStatus? status,
        Guid? originSalesOrderId,
        int skip,
        int take,
        CancellationToken ct = default);
    Task AddAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
}
