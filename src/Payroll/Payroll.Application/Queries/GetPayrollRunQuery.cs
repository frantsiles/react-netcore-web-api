using Api.Application.Common.Interfaces;
using MediatR;
using Payroll.Application.Commands;
using Payroll.Application.DTOs;
using Payroll.Domain.Services;

namespace Payroll.Application.Queries;

public record GetPayrollRunQuery(Guid RunId) : IRequest<PayrollRunDto?>;
public record ListPayrollRunsQuery(int Skip = 0, int Take = 50)
    : IRequest<IReadOnlyList<PayrollRunDto>>;

public class GetPayrollRunHandler(IPayrollRunRepository repo)
    : IRequestHandler<GetPayrollRunQuery, PayrollRunDto?>
{
    public async Task<PayrollRunDto?> Handle(GetPayrollRunQuery req, CancellationToken ct)
    {
        var run = await repo.GetByIdAsync(req.RunId, ct);
        return run is null ? null : CreatePayrollRunHandler.ToDto(run);
    }
}

public class ListPayrollRunsHandler(IPayrollRunRepository repo, ITenantContext tenant)
    : IRequestHandler<ListPayrollRunsQuery, IReadOnlyList<PayrollRunDto>>
{
    public async Task<IReadOnlyList<PayrollRunDto>> Handle(ListPayrollRunsQuery req, CancellationToken ct)
    {
        var runs = await repo.ListByTenantAsync(tenant.TenantId, req.Skip, req.Take, ct);
        return runs.Select(CreatePayrollRunHandler.ToDto).ToList();
    }
}
