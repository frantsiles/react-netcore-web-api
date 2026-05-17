using Api.Domain.Common;

namespace HR.Domain.Departments;

public class Department : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid? ParentDepartmentId { get; private set; }
    public string? CostCenter { get; private set; }
    public bool IsActive { get; private set; }

    private Department() { }

    public static Department Create(
        Guid tenantId,
        string code,
        string name,
        Guid? parentDepartmentId = null,
        string? costCenter = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Department code cannot be empty.");

        return new Department
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            ParentDepartmentId = parentDepartmentId,
            CostCenter = costCenter,
            IsActive = true
        };
    }

    public void Update(string name, Guid? parentDepartmentId, string? costCenter)
    {
        Name = name;
        ParentDepartmentId = parentDepartmentId;
        CostCenter = costCenter;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("Department is already inactive.");
        IsActive = false;
        SetUpdatedAt();
    }
}
