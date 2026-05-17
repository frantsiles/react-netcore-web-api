using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Invoicing;

public class SearchInvoicesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchInvoicesBffQuery, IReadOnlyList<InvoiceBffDto>>
{
    public async Task<IReadOnlyList<InvoiceBffDto>> Handle(
        SearchInvoicesBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (request.CustomerId.HasValue) parts.Add($"customerId={request.CustomerId}");
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"status={request.Status}");
        if (request.OriginSalesOrderId.HasValue) parts.Add($"originSalesOrderId={request.OriginSalesOrderId}");
        parts.Add($"skip={request.Skip}");
        parts.Add($"take={request.Take}");

        var url = $"api/invoicing/invoices?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<InvoiceBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class GetInvoiceByIdBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetInvoiceByIdBffQuery, InvoiceBffDto?>
{
    public async Task<InvoiceBffDto?> Handle(GetInvoiceByIdBffQuery request, CancellationToken ct)
        => await apiClient.GetAsync<InvoiceBffDto>(
            $"api/invoicing/invoices/{request.InvoiceId}", request.Token, ct);
}

public class ConvertOrderToInvoiceBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ConvertOrderToInvoiceBffCommand, InvoiceBffDto>
{
    public async Task<InvoiceBffDto> Handle(ConvertOrderToInvoiceBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            SalesOrderId = request.SalesOrderId,
            DueDate      = request.DueDate,
            Notes        = request.Notes,
        };
        var result = await apiClient.PostAsync<object, InvoiceBffDto>(
            "api/invoicing/invoices/convert", body, request.Token, ct);
        return result!;
    }
}

public class IssueInvoiceBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<IssueInvoiceBffCommand, InvoiceBffDto>
{
    public async Task<InvoiceBffDto> Handle(IssueInvoiceBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, InvoiceBffDto>(
            $"api/invoicing/invoices/{request.InvoiceId}/issue", new { }, request.Token, ct);
        return result!;
    }
}

public class RecordInvoicePaymentBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RecordInvoicePaymentBffCommand, InvoiceBffDto>
{
    public async Task<InvoiceBffDto> Handle(RecordInvoicePaymentBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            Amount       = request.Amount,
            CurrencyCode = request.CurrencyCode,
            PaidAt       = request.PaidAt,
            Reference    = request.Reference,
        };
        var result = await apiClient.PostAsync<object, InvoiceBffDto>(
            $"api/invoicing/invoices/{request.InvoiceId}/payments", body, request.Token, ct);
        return result!;
    }
}

public class CancelInvoiceBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CancelInvoiceBffCommand, InvoiceBffDto>
{
    public async Task<InvoiceBffDto> Handle(CancelInvoiceBffCommand request, CancellationToken ct)
    {
        var body = new { Reason = request.Reason };
        var result = await apiClient.PostAsync<object, InvoiceBffDto>(
            $"api/invoicing/invoices/{request.InvoiceId}/cancel", body, request.Token, ct);
        return result!;
    }
}
