using Api.Domain.Common;

namespace HR.Domain.Contracts;

public enum ContractStatus { Active, Expired, Terminated }

public class Contract : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; private set; }
    public string ContractNumber { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public decimal GrossSalary { get; private set; }
    public string CurrencyCode { get; private set; } = default!;
    public ContractStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private Contract() { }

    public static Contract Create(
        Guid tenantId,
        Guid employeeId,
        string contractNumber,
        DateOnly startDate,
        decimal grossSalary,
        string currencyCode,
        DateOnly? endDate = null,
        string? notes = null)
    {
        if (grossSalary < 0)
            throw new DomainException("Salary cannot be negative.");

        return new Contract
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            ContractNumber = contractNumber,
            StartDate = startDate,
            EndDate = endDate,
            GrossSalary = grossSalary,
            CurrencyCode = currencyCode,
            Status = ContractStatus.Active,
            Notes = notes
        };
    }

    public void Terminate(string? notes = null)
    {
        if (Status != ContractStatus.Active)
            throw new DomainException($"Cannot terminate a contract in status '{Status}'.");
        Status = ContractStatus.Terminated;
        Notes = notes;
        SetUpdatedAt();
    }

    public void MarkExpired()
    {
        if (Status != ContractStatus.Active)
            throw new DomainException("Contract is not active.");
        Status = ContractStatus.Expired;
        SetUpdatedAt();
    }
}
