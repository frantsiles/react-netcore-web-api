using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Tenants;

public class ListTenantsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListTenantsBffQuery, IReadOnlyList<TenantBffDto>>
{
    public async Task<IReadOnlyList<TenantBffDto>> Handle(ListTenantsBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Name))   parts.Add($"name={Uri.EscapeDataString(request.Name)}");
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"status={request.Status}");
        if (!string.IsNullOrWhiteSpace(request.Plan))   parts.Add($"plan={request.Plan}");
        var url = "api/admin/tenants" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        var result = await apiClient.GetAsync<List<TenantBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class GetTenantByIdBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetTenantByIdBffQuery, TenantBffDto?>
{
    public async Task<TenantBffDto?> Handle(GetTenantByIdBffQuery request, CancellationToken ct)
        => await apiClient.GetAsync<TenantBffDto>($"api/admin/tenants/{request.TenantId}", request.Token, ct);
}

public class CreateTenantBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateTenantBffCommand, TenantBffDto>
{
    public async Task<TenantBffDto> Handle(CreateTenantBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            Name         = request.Name,
            Slug         = request.Slug,
            CountryCode  = request.CountryCode,
            CurrencyCode = request.CurrencyCode,
            Plan         = request.Plan,
        };
        var result = await apiClient.PostAsync<object, TenantBffDto>("api/admin/tenants", body, request.Token, ct);
        return result!;
    }
}

public class UpdateTenantBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<UpdateTenantBffCommand, TenantBffDto>
{
    public async Task<TenantBffDto> Handle(UpdateTenantBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            Name         = request.Name,
            CountryCode  = request.CountryCode,
            CurrencyCode = request.CurrencyCode,
            Plan         = request.Plan,
        };
        var result = await apiClient.PutAsync<object, TenantBffDto>($"api/admin/tenants/{request.TenantId}", body, request.Token, ct);
        return result!;
    }
}

public class SuspendTenantBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<SuspendTenantBffCommand, TenantBffDto>
{
    public async Task<TenantBffDto> Handle(SuspendTenantBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, TenantBffDto>(
            $"api/admin/tenants/{request.TenantId}/suspend", new { }, request.Token, ct);
        return result!;
    }
}

public class ReactivateTenantBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ReactivateTenantBffCommand, TenantBffDto>
{
    public async Task<TenantBffDto> Handle(ReactivateTenantBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, TenantBffDto>(
            $"api/admin/tenants/{request.TenantId}/reactivate", new { }, request.Token, ct);
        return result!;
    }
}

public class SetTenantSettingBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<SetTenantSettingBffCommand, TenantBffDto>
{
    public async Task<TenantBffDto> Handle(SetTenantSettingBffCommand request, CancellationToken ct)
    {
        var body = new { Value = request.Value };
        var result = await apiClient.PutAsync<object, TenantBffDto>(
            $"api/admin/tenants/{request.TenantId}/settings/{Uri.EscapeDataString(request.Key)}", body, request.Token, ct);
        return result!;
    }
}

public class RemoveTenantSettingBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RemoveTenantSettingBffCommand, TenantBffDto>
{
    public async Task<TenantBffDto> Handle(RemoveTenantSettingBffCommand request, CancellationToken ct)
    {
        await apiClient.DeleteAsync(
            $"api/admin/tenants/{request.TenantId}/settings/{Uri.EscapeDataString(request.Key)}", request.Token, ct);
        var result = await apiClient.GetAsync<TenantBffDto>($"api/admin/tenants/{request.TenantId}", request.Token, ct);
        return result!;
    }
}
