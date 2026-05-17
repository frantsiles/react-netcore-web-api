using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Tax;

public class ListTaxRatesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListTaxRatesBffQuery, IReadOnlyList<TaxRateBffDto>>
{
    public async Task<IReadOnlyList<TaxRateBffDto>> Handle(
        ListTaxRatesBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"status={request.Status}");
        if (!string.IsNullOrWhiteSpace(request.Applicability)) parts.Add($"applicability={request.Applicability}");

        var url = parts.Count > 0
            ? $"api/tax/rates?{string.Join("&", parts)}"
            : "api/tax/rates";
        var result = await apiClient.GetAsync<List<TaxRateBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class CalculateTaxBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<CalculateTaxBffQuery, TaxCalculationBffDto>
{
    public async Task<TaxCalculationBffDto> Handle(CalculateTaxBffQuery request, CancellationToken ct)
    {
        var url = $"api/tax/rates/calculate?taxCode={Uri.EscapeDataString(request.TaxCode)}&baseAmount={request.BaseAmount}";
        var result = await apiClient.GetAsync<TaxCalculationBffDto>(url, request.Token, ct);
        return result!;
    }
}

public class CreateTaxRateBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateTaxRateBffCommand, TaxRateBffDto>
{
    public async Task<TaxRateBffDto> Handle(CreateTaxRateBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            Code          = request.Code,
            Name          = request.Name,
            Rate          = request.Rate,
            Applicability = request.Applicability,
            Description   = request.Description,
        };
        var result = await apiClient.PostAsync<object, TaxRateBffDto>(
            "api/tax/rates", body, request.Token, ct);
        return result!;
    }
}

public class UpdateTaxRateBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<UpdateTaxRateBffCommand, TaxRateBffDto>
{
    public async Task<TaxRateBffDto> Handle(UpdateTaxRateBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            Name          = request.Name,
            Rate          = request.Rate,
            Applicability = request.Applicability,
            Description   = request.Description,
        };
        var result = await apiClient.PutAsync<object, TaxRateBffDto>(
            $"api/tax/rates/{request.Id}", body, request.Token, ct);
        return result!;
    }
}

public class DeactivateTaxRateBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<DeactivateTaxRateBffCommand, TaxRateBffDto>
{
    public async Task<TaxRateBffDto> Handle(DeactivateTaxRateBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, TaxRateBffDto>(
            $"api/tax/rates/{request.Id}/deactivate", new { }, request.Token, ct);
        return result!;
    }
}
