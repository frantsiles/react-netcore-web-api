using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Sales;

public class SearchQuotesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchQuotesBffQuery, IReadOnlyList<QuoteBffDto>>
{
    public async Task<IReadOnlyList<QuoteBffDto>> Handle(
        SearchQuotesBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (request.CustomerId.HasValue)
            parts.Add($"customerId={request.CustomerId}");
        if (!string.IsNullOrWhiteSpace(request.Status))
            parts.Add($"status={request.Status}");
        parts.Add($"skip={request.Skip}");
        parts.Add($"take={request.Take}");

        var url = $"api/sales/quotes?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<QuoteBffDto>>(url, request.BearerToken, ct);
        return result ?? [];
    }
}

public class CreateQuoteBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateQuoteBffCommand, QuoteBffDto>
{
    public async Task<QuoteBffDto> Handle(
        CreateQuoteBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            CustomerId   = request.CustomerId,
            ValidUntil   = request.ValidUntil,
            CurrencyCode = request.CurrencyCode,
            CountryCode  = request.CountryCode,
            Notes        = request.Notes,
        };
        var result = await apiClient.PostAsync<object, QuoteBffDto>(
            "api/sales/quotes", body, request.BearerToken, ct);
        return result!;
    }
}

public class AddQuoteLineBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<AddQuoteLineBffCommand, QuoteBffDto>
{
    public async Task<QuoteBffDto> Handle(
        AddQuoteLineBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            CatalogItemId   = request.CatalogItemId,
            SKU             = request.SKU,
            ItemName        = request.ItemName,
            Quantity        = request.Quantity,
            UnitPrice       = request.UnitPrice,
            CurrencyCode    = request.CurrencyCode,
            DiscountPercent = request.DiscountPercent,
            Notes           = request.Notes,
        };
        var result = await apiClient.PostAsync<object, QuoteBffDto>(
            $"api/sales/quotes/{request.QuoteId}/lines", body, request.BearerToken, ct);
        return result!;
    }
}

public class SendQuoteBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<SendQuoteBffCommand, QuoteBffDto>
{
    public async Task<QuoteBffDto> Handle(SendQuoteBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, QuoteBffDto>(
            $"api/sales/quotes/{request.QuoteId}/send", new { }, request.BearerToken, ct);
        return result!;
    }
}

public class AcceptQuoteBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<AcceptQuoteBffCommand, QuoteBffDto>
{
    public async Task<QuoteBffDto> Handle(AcceptQuoteBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, QuoteBffDto>(
            $"api/sales/quotes/{request.QuoteId}/accept", new { }, request.BearerToken, ct);
        return result!;
    }
}

public class RejectQuoteBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RejectQuoteBffCommand, QuoteBffDto>
{
    public async Task<QuoteBffDto> Handle(RejectQuoteBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, QuoteBffDto>(
            $"api/sales/quotes/{request.QuoteId}/reject", new { }, request.BearerToken, ct);
        return result!;
    }
}

public class ConvertQuoteToOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ConvertQuoteToOrderBffCommand, SalesOrderBffDto>
{
    public async Task<SalesOrderBffDto> Handle(
        ConvertQuoteToOrderBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            RequestedDeliveryDate = request.RequestedDeliveryDate,
            Notes = request.Notes,
        };
        var result = await apiClient.PostAsync<object, SalesOrderBffDto>(
            $"api/sales/quotes/{request.QuoteId}/convert-to-order", body, request.BearerToken, ct);
        return result!;
    }
}

public class SearchSalesOrdersBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchSalesOrdersBffQuery, IReadOnlyList<SalesOrderBffDto>>
{
    public async Task<IReadOnlyList<SalesOrderBffDto>> Handle(
        SearchSalesOrdersBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (request.CustomerId.HasValue)
            parts.Add($"customerId={request.CustomerId}");
        if (!string.IsNullOrWhiteSpace(request.Status))
            parts.Add($"status={request.Status}");
        parts.Add($"skip={request.Skip}");
        parts.Add($"take={request.Take}");

        var url = $"api/sales/orders?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<SalesOrderBffDto>>(url, request.BearerToken, ct);
        return result ?? [];
    }
}

public class ConfirmSalesOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ConfirmSalesOrderBffCommand, SalesOrderBffDto>
{
    public async Task<SalesOrderBffDto> Handle(
        ConfirmSalesOrderBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, SalesOrderBffDto>(
            $"api/sales/orders/{request.OrderId}/confirm", new { }, request.BearerToken, ct);
        return result!;
    }
}

public class CancelSalesOrderBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CancelSalesOrderBffCommand, SalesOrderBffDto>
{
    public async Task<SalesOrderBffDto> Handle(
        CancelSalesOrderBffCommand request, CancellationToken ct)
    {
        var body = new { Reason = request.Reason };
        var result = await apiClient.PostAsync<object, SalesOrderBffDto>(
            $"api/sales/orders/{request.OrderId}/cancel", body, request.BearerToken, ct);
        return result!;
    }
}
