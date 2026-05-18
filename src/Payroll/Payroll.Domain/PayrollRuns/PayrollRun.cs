using Api.Domain.Common;

namespace Payroll.Domain.PayrollRuns;

public class PayrollRun : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string RunNumber { get; private set; } = "";
    public PayrollPeriodType PeriodType { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public PayrollRunStatus Status { get; private set; }

    public decimal TotalGross { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal TotalNet { get; private set; }
    public decimal TotalEmployerCost { get; private set; }
    public int EmployeeCount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private readonly List<PayrollEntry> _entries = [];
    public IReadOnlyList<PayrollEntry> Entries => _entries.AsReadOnly();

    private PayrollRun() : base() { }

    public static PayrollRun Create(
        Guid tenantId, string runNumber,
        PayrollPeriodType periodType,
        DateOnly periodStart, DateOnly periodEnd,
        string currencyCode)
    {
        return new PayrollRun
        {
            TenantId = tenantId,
            RunNumber = runNumber,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Status = PayrollRunStatus.Draft,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void AddEntry(PayrollEntry entry)
    {
        if (Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Cannot modify a confirmed payroll run.");
        _entries.Add(entry);
        RecalculateTotals();
    }

    public void Confirm()
    {
        if (Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Only draft runs can be confirmed.");
        if (_entries.Count == 0)
            throw new InvalidOperationException("Cannot confirm an empty payroll run.");
        Status = PayrollRunStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void MarkPaid()
    {
        if (Status != PayrollRunStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed runs can be marked as paid.");
        Status = PayrollRunStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        TotalGross = _entries.Sum(e => e.TotalGross);
        TotalDeductions = _entries.Sum(e => e.TotalDeductions);
        TotalNet = _entries.Sum(e => e.NetPay);
        TotalEmployerCost = _entries.Sum(e => e.TotalLaborCost);
        EmployeeCount = _entries.Count;
    }
}
