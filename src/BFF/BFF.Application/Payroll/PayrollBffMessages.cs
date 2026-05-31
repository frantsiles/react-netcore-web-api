namespace BFF.Application.Payroll;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record PayrollEntryBffDto(
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

public record PayrollRunBffDto(
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
    IReadOnlyList<PayrollEntryBffDto> Entries);

// ── Commands / Queries ────────────────────────────────────────────────────────

public record CreatePayrollRunBffCommand(
    string Token,
    string PeriodType,
    string PeriodStart,
    string PeriodEnd,
    string CurrencyCode) : MediatR.IRequest<PayrollRunBffDto>;

public record GetPayrollRunBffQuery(string Token, Guid RunId)
    : MediatR.IRequest<PayrollRunBffDto?>;

public record ListPayrollRunsBffQuery(string Token, int Skip, int Take)
    : MediatR.IRequest<List<PayrollRunBffDto>>;

public record ConfirmPayrollRunBffCommand(string Token, Guid RunId)
    : MediatR.IRequest<PayrollRunBffDto>;

public record DownloadPaystubBffQuery(string Token, Guid RunId, Guid EntryId)
    : MediatR.IRequest<(byte[] Content, string ContentType, string FileName)>;

public record MarkPayrollRunPaidBffCommand(string Token, Guid RunId)
    : MediatR.IRequest<PayrollRunBffDto>;

public record DownloadCcssReportBffQuery(string Token, Guid RunId)
    : MediatR.IRequest<(byte[] Content, string ContentType, string FileName)>;
