namespace Payroll.Application.DTOs;

public record PayrollEntryDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    decimal BaseSalary,
    decimal GrossSalary,
    decimal OvertimePay,
    decimal TotalGross,
    decimal CcssEmployee,
    decimal BancoPopular,
    decimal IncomeTax,
    decimal TotalDeductions,
    decimal NetPay,
    decimal CcssEmployer,
    decimal InsEmployer,
    decimal Fcl,
    decimal TotalEmployerContribution,
    decimal TotalLaborCost);

public record PayrollRunDto(
    Guid Id,
    string RunNumber,
    string PeriodType,
    string PeriodStart,
    string PeriodEnd,
    string CurrencyCode,
    string Status,
    int EmployeeCount,
    decimal TotalGross,
    decimal TotalDeductions,
    decimal TotalNet,
    decimal TotalEmployerCost,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? PaidAt,
    IReadOnlyList<PayrollEntryDto> Entries);
