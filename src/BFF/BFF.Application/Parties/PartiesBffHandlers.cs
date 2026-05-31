using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Parties;

public class SearchPartiesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchPartiesBffQuery, IReadOnlyList<PartyBffDto>>
{
    public async Task<IReadOnlyList<PartyBffDto>> Handle(
        SearchPartiesBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.LegalName))
            parts.Add($"legalName={Uri.EscapeDataString(request.LegalName)}");
        if (!string.IsNullOrWhiteSpace(request.RoleType))
            parts.Add($"roleType={request.RoleType}");
        if (request.IsActive is not null)
            parts.Add($"isActive={request.IsActive.ToString()!.ToLower()}");
        parts.Add($"skip={request.Skip}");
        parts.Add($"take={request.Take}");

        var url = $"api/parties?{string.Join("&", parts)}";
        var result = await apiClient.GetAsync<List<PartyBffDto>>(url, request.BearerToken, ct);
        return result ?? [];
    }
}

public class GetPartyByIdBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetPartyByIdBffQuery, PartyBffDto?>
{
    public async Task<PartyBffDto?> Handle(
        GetPartyByIdBffQuery request, CancellationToken ct)
        => await apiClient.GetAsync<PartyBffDto>(
            $"api/parties/{request.PartyId}", request.BearerToken, ct);
}

public class RegisterPartyBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RegisterPartyBffCommand, PartyBffDto>
{
    public async Task<PartyBffDto> Handle(
        RegisterPartyBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            LegalName = request.LegalName,
            TradeName = request.TradeName,
            PartyType = request.PartyType,
            CountryCode = request.CountryCode,
            FirstRoleType = request.FirstRoleType,
            TaxId = request.TaxId,
            CreditLimit = request.CreditLimit,
            CreditLimitCurrency = request.CreditLimitCurrency,
            PaymentTermsDays = request.PaymentTermsDays,
            EmployeeNumber = request.EmployeeNumber,
        };
        var result = await apiClient.PostAsync<object, PartyBffDto>(
            "api/parties", body, request.BearerToken, ct);
        return result!;
    }
}

public class UpdatePartyProfileBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<UpdatePartyProfileBffCommand, PartyBffDto>
{
    public async Task<PartyBffDto> Handle(
        UpdatePartyProfileBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            LegalName = request.LegalName,
            TradeName = request.TradeName,
            TaxId = request.TaxId,
        };
        var result = await apiClient.PatchAsync<object, PartyBffDto>(
            $"api/parties/{request.PartyId}/profile", body, request.BearerToken, ct);
        return result!;
    }
}

public class DeactivatePartyBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<DeactivatePartyBffCommand, Unit>
{
    public async Task<Unit> Handle(
        DeactivatePartyBffCommand request, CancellationToken ct)
    {
        await apiClient.DeleteAsync($"api/parties/{request.PartyId}", request.BearerToken, ct);
        return Unit.Value;
    }
}

public class ReactivatePartyBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ReactivatePartyBffCommand, Unit>
{
    public async Task<Unit> Handle(
        ReactivatePartyBffCommand request, CancellationToken ct)
    {
        await apiClient.PostAsync<object>(
            $"api/parties/{request.PartyId}/reactivate", new { }, request.BearerToken, ct);
        return Unit.Value;
    }
}
