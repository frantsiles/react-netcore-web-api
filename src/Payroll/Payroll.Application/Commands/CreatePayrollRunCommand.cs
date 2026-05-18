using Api.Application.Common.Interfaces;
using HR.Domain.Contracts;
using HR.Domain.Employees;
using HR.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.DTOs;
using Payroll.Application.Services;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.Services;

namespace Payroll.Application.Commands;

public record CreatePayrollRunCommand(
    string PeriodType,
    string PeriodStart,
    string PeriodEnd,
    string CurrencyCode) : IRequest<PayrollRunDto>;

public class CreatePayrollRunHandler(
    IPayrollRunRepository repo,
    HrDbContext hrDb,
    ITenantContext tenant)
    : IRequestHandler<CreatePayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(CreatePayrollRunCommand req, CancellationToken ct)
    {
        var tenantId = tenant.TenantId;
        var periodType = Enum.Parse<PayrollPeriodType>(req.PeriodType, ignoreCase: true);
        var start = DateOnly.Parse(req.PeriodStart);
        var end = DateOnly.Parse(req.PeriodEnd);

        var employees = await hrDb.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.Status == EmployeeStatus.Active)
            .ToListAsync(ct);

        var contracts = await hrDb.Contracts.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == ContractStatus.Active)
            .ToListAsync(ct);

        var contractMap = contracts.ToDictionary(c => c.EmployeeId);

        var runNumber = $"PLAN-{start:yyyyMMdd}-{end:yyyyMMdd}";
        var run = PayrollRun.Create(tenantId, runNumber, periodType, start, end, req.CurrencyCode);

        foreach (var emp in employees)
        {
            if (!contractMap.TryGetValue(emp.Id, out var contract)) continue;
            var entry = PayrollCalculatorCR.Calculate(
                emp.Id, emp.EmployeeNumber, emp.FullName,
                contract.GrossSalary, periodType);
            run.AddEntry(entry);
        }

        await repo.AddAsync(run, ct);
        await repo.SaveChangesAsync(ct);
        return ToDto(run);
    }

    internal static PayrollRunDto ToDto(PayrollRun r) => new(
        r.Id, r.RunNumber, r.PeriodType.ToString(),
        r.PeriodStart.ToString("yyyy-MM-dd"), r.PeriodEnd.ToString("yyyy-MM-dd"),
        r.CurrencyCode, r.Status.ToString(),
        r.EmployeeCount, r.TotalGross, r.TotalDeductions, r.TotalNet, r.TotalEmployerCost,
        r.CreatedAt, r.ConfirmedAt, r.PaidAt,
        r.Entries.Select(ToEntryDto).ToList().AsReadOnly());

    private static PayrollEntryDto ToEntryDto(PayrollEntry e) => new(
        e.Id, e.EmployeeId, e.EmployeeNumber, e.EmployeeName,
        e.BaseSalary, e.GrossSalary, e.OvertimePay, e.TotalGross,
        e.CcssEmployee, e.BancoPopular, e.IncomeTax, e.TotalDeductions, e.NetPay,
        e.CcssEmployer, e.InsEmployer, e.Fcl, e.TotalEmployerContribution, e.TotalLaborCost);
}
