using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Payroll;

public class CreatePayrollRunBffHandler(IApiClient apiClient)
    : IRequestHandler<CreatePayrollRunBffCommand, PayrollRunBffDto>
{
    public async Task<PayrollRunBffDto> Handle(CreatePayrollRunBffCommand req, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, PayrollRunBffDto>(
            "api/payroll/runs",
            new { req.PeriodType, req.PeriodStart, req.PeriodEnd, req.CurrencyCode },
            req.Token, ct);
        return result!;
    }
}

public class GetPayrollRunBffHandler(IApiClient apiClient)
    : IRequestHandler<GetPayrollRunBffQuery, PayrollRunBffDto?>
{
    public Task<PayrollRunBffDto?> Handle(GetPayrollRunBffQuery req, CancellationToken ct)
        => apiClient.GetAsync<PayrollRunBffDto>($"api/payroll/runs/{req.RunId}", req.Token, ct);
}

public class ListPayrollRunsBffHandler(IApiClient apiClient)
    : IRequestHandler<ListPayrollRunsBffQuery, List<PayrollRunBffDto>>
{
    public async Task<List<PayrollRunBffDto>> Handle(ListPayrollRunsBffQuery req, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<List<PayrollRunBffDto>>(
            $"api/payroll/runs?skip={req.Skip}&take={req.Take}",
            req.Token, ct);
        return result ?? [];
    }
}

public class ConfirmPayrollRunBffHandler(IApiClient apiClient)
    : IRequestHandler<ConfirmPayrollRunBffCommand, PayrollRunBffDto>
{
    public async Task<PayrollRunBffDto> Handle(ConfirmPayrollRunBffCommand req, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, PayrollRunBffDto>(
            $"api/payroll/runs/{req.RunId}/confirm",
            new { }, req.Token, ct);
        return result!;
    }
}

public class DownloadPaystubBffHandler(IApiClient apiClient)
    : IRequestHandler<DownloadPaystubBffQuery, (byte[] Content, string ContentType, string FileName)>
{
    public Task<(byte[] Content, string ContentType, string FileName)> Handle(
        DownloadPaystubBffQuery req, CancellationToken ct)
        => apiClient.GetFileAsync(
            $"api/payroll/runs/{req.RunId}/entries/{req.EntryId}/paystub",
            req.Token, ct);
}
