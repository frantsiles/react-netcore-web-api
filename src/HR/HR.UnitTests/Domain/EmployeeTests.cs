using Api.Domain.Common;
using FluentAssertions;
using HR.Domain.DomainEvents;
using HR.Domain.Employees;

namespace HR.UnitTests.Domain;

public class EmployeeTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DeptId = Guid.NewGuid();
    private static readonly DateOnly HireDate = new(2024, 1, 15);

    private static Employee NewEmployee() =>
        Employee.Hire(TenantId, "EMP-001", "John", "Doe", "john.doe@acme.com",
            DeptId, "Software Engineer", EmploymentType.FullTime, HireDate);

    [Fact]
    public void Hire_ValidArgs_SetsActiveStatus()
    {
        var emp = NewEmployee();

        emp.EmployeeNumber.Should().Be("EMP-001");
        emp.Status.Should().Be(EmployeeStatus.Active);
        emp.FullName.Should().Be("John Doe");
        emp.HireDate.Should().Be(HireDate);
    }

    [Fact]
    public void Hire_RaisesEmployeeHiredEvent()
    {
        var emp = NewEmployee();

        emp.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EmployeeHiredEvent>();
    }

    [Fact]
    public void Terminate_ActiveEmployee_SetsTerminated()
    {
        var emp = NewEmployee();
        var terminationDate = new DateOnly(2025, 3, 31);

        emp.Terminate(terminationDate, "Resignation");

        emp.Status.Should().Be(EmployeeStatus.Terminated);
        emp.TerminationDate.Should().Be(terminationDate);
        emp.TerminationReason.Should().Be("Resignation");
    }

    [Fact]
    public void Terminate_RaisesEmployeeTerminatedEvent()
    {
        var emp = NewEmployee();
        emp.ClearDomainEvents();

        emp.Terminate(new DateOnly(2025, 3, 31), "Resignation");

        emp.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EmployeeTerminatedEvent>();
    }

    [Fact]
    public void Terminate_AlreadyTerminated_ThrowsDomainException()
    {
        var emp = NewEmployee();
        emp.Terminate(new DateOnly(2025, 3, 31), "Resignation");

        var act = () => emp.Terminate(new DateOnly(2025, 4, 1), "Other");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetOnLeave_ActiveEmployee_SetsOnLeave()
    {
        var emp = NewEmployee();

        emp.SetOnLeave();

        emp.Status.Should().Be(EmployeeStatus.OnLeave);
    }

    [Fact]
    public void ReturnFromLeave_OnLeaveEmployee_SetsActive()
    {
        var emp = NewEmployee();
        emp.SetOnLeave();

        emp.ReturnFromLeave();

        emp.Status.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public void SetOnLeave_NonActiveEmployee_ThrowsDomainException()
    {
        var emp = NewEmployee();
        emp.Terminate(new DateOnly(2025, 3, 31), "Resignation");

        var act = () => emp.SetOnLeave();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateProfile_ChangesFields()
    {
        var emp = NewEmployee();
        var newDept = Guid.NewGuid();

        emp.UpdateProfile("Jane", "Smith", "jane@acme.com", "+1234", "Senior Engineer", newDept, null);

        emp.FirstName.Should().Be("Jane");
        emp.LastName.Should().Be("Smith");
        emp.DepartmentId.Should().Be(newDept);
        emp.FullName.Should().Be("Jane Smith");
    }
}
