using Payroll.Domain.PayrollRuns;

namespace Payroll.Application.Services;

/// <summary>
/// Costa Rica payroll calculator based on:
/// - CCSS employee: 10.67% (SEM 5.5% + IVM 2.84% + BSP 0.5% + BN 0.5% + INS 0.25% + ASFA 1.08%)
/// - Banco Popular employee: 1%
/// - Income tax (art. 33 LISR): progressive monthly brackets
/// - CCSS employer: 26.67%
/// - INS employer: 1%
/// - FCL (Fondo de Capitalización Laboral): 3%
/// Brackets updated periodically by MHDA; these reflect 2025 values.
/// </summary>
public static class PayrollCalculatorCR
{
    private const decimal CcssEmployeeRate = 0.1067m;
    private const decimal BancoPopularRate = 0.01m;
    private const decimal CcssEmployerRate = 0.2667m;
    private const decimal InsEmployerRate = 0.01m;
    private const decimal FclRate = 0.03m;

    // Monthly income tax brackets (CRC), art. 33 LISR 2025
    private static readonly (decimal UpTo, decimal Rate)[] MonthlyBrackets =
    [
        (941_000m,   0.00m),
        (1_381_000m, 0.10m),
        (2_423_000m, 0.15m),
        (4_845_000m, 0.20m),
        (decimal.MaxValue, 0.25m),
    ];

    public static PayrollEntry Calculate(
        Guid employeeId,
        string employeeNumber,
        string employeeName,
        decimal monthlySalary,
        PayrollPeriodType periodType,
        decimal overtimePay = 0m)
    {
        // Pro-rate salary: biweekly = monthly * 12 / 24
        var grossSalary = periodType == PayrollPeriodType.Biweekly
            ? Math.Round(monthlySalary / 2m, 2)
            : monthlySalary;

        var totalGross = grossSalary + overtimePay;

        // Deductions on total gross
        var ccssEmployee = Math.Round(totalGross * CcssEmployeeRate, 2);
        var bancoPopular = Math.Round(totalGross * BancoPopularRate, 2);

        // Income tax: convert to monthly equivalent for bracket lookup
        var monthlyEquivalent = periodType == PayrollPeriodType.Biweekly
            ? totalGross * 2m
            : totalGross;
        var monthlyTax = CalculateProgressiveTax(monthlyEquivalent);
        var incomeTax = periodType == PayrollPeriodType.Biweekly
            ? Math.Round(monthlyTax / 2m, 2)
            : Math.Round(monthlyTax, 2);

        // Employer contributions on total gross
        var ccssEmployer = Math.Round(totalGross * CcssEmployerRate, 2);
        var insEmployer = Math.Round(totalGross * InsEmployerRate, 2);
        var fcl = Math.Round(totalGross * FclRate, 2);

        return PayrollEntry.Create(
            employeeId, employeeNumber, employeeName,
            monthlySalary, grossSalary, overtimePay,
            ccssEmployee, bancoPopular, incomeTax,
            ccssEmployer, insEmployer, fcl);
    }

    private static decimal CalculateProgressiveTax(decimal monthlyGross)
    {
        var tax = 0m;
        var prev = 0m;

        foreach (var (upTo, rate) in MonthlyBrackets)
        {
            if (rate == 0m)
            {
                prev = upTo;
                continue;
            }
            if (monthlyGross <= prev) break;

            var taxable = Math.Min(monthlyGross, upTo) - prev;
            tax += taxable * rate;
            prev = upTo;
            if (monthlyGross <= upTo) break;
        }

        return tax;
    }
}
