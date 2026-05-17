using Api.Domain.Common;
using HR.Domain.DomainEvents;

namespace HR.Domain.Employees;

public class Employee : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string EmployeeNumber { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string JobTitle { get; private set; } = default!;
    public EmploymentType EmploymentType { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public DateOnly HireDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }
    public string? TerminationReason { get; private set; }
    public string? ManagerEmployeeId { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private Employee() { }

    public static Employee Hire(
        Guid tenantId,
        string employeeNumber,
        string firstName,
        string lastName,
        string email,
        Guid departmentId,
        string jobTitle,
        EmploymentType employmentType,
        DateOnly hireDate,
        string? phone = null,
        string? managerEmployeeId = null)
    {
        var emp = new Employee
        {
            TenantId = tenantId,
            EmployeeNumber = employeeNumber,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            DepartmentId = departmentId,
            JobTitle = jobTitle,
            EmploymentType = employmentType,
            Status = EmployeeStatus.Active,
            HireDate = hireDate,
            ManagerEmployeeId = managerEmployeeId
        };
        emp.RaiseDomainEvent(new EmployeeHiredEvent(emp.Id, tenantId, employeeNumber, departmentId));
        return emp;
    }

    public void UpdateProfile(string firstName, string lastName, string email, string? phone, string jobTitle, Guid departmentId, string? managerEmployeeId)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        JobTitle = jobTitle;
        DepartmentId = departmentId;
        ManagerEmployeeId = managerEmployeeId;
        SetUpdatedAt();
    }

    public void Terminate(DateOnly terminationDate, string reason)
    {
        if (Status == EmployeeStatus.Terminated)
            throw new DomainException("Employee is already terminated.");
        Status = EmployeeStatus.Terminated;
        TerminationDate = terminationDate;
        TerminationReason = reason;
        SetUpdatedAt();
        RaiseDomainEvent(new EmployeeTerminatedEvent(Id, TenantId, EmployeeNumber, terminationDate));
    }

    public void SetOnLeave()
    {
        if (Status != EmployeeStatus.Active)
            throw new DomainException("Only active employees can be set on leave.");
        Status = EmployeeStatus.OnLeave;
        SetUpdatedAt();
    }

    public void ReturnFromLeave()
    {
        if (Status != EmployeeStatus.OnLeave)
            throw new DomainException("Employee is not on leave.");
        Status = EmployeeStatus.Active;
        SetUpdatedAt();
    }
}
