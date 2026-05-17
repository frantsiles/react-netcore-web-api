using Api.Domain.Common;
using FluentAssertions;
using HR.Domain.Contracts;

namespace HR.UnitTests.Domain;

public class ContractTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static Contract NewContract() =>
        Contract.Create(TenantId, EmployeeId, "CTR-001",
            new DateOnly(2024, 1, 1), 5000m, "USD", new DateOnly(2024, 12, 31));

    [Fact]
    public void Create_ValidArgs_SetsActive()
    {
        var contract = NewContract();

        contract.Status.Should().Be(ContractStatus.Active);
        contract.GrossSalary.Should().Be(5000m);
        contract.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Create_NegativeSalary_ThrowsDomainException()
    {
        var act = () => Contract.Create(TenantId, EmployeeId, "CTR-X",
            new DateOnly(2024, 1, 1), -100m, "USD");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Terminate_Active_SetsTerminated()
    {
        var contract = NewContract();

        contract.Terminate("Early exit");

        contract.Status.Should().Be(ContractStatus.Terminated);
        contract.Notes.Should().Be("Early exit");
    }

    [Fact]
    public void Terminate_AlreadyTerminated_ThrowsDomainException()
    {
        var contract = NewContract();
        contract.Terminate();

        var act = () => contract.Terminate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkExpired_Active_SetsExpired()
    {
        var contract = NewContract();

        contract.MarkExpired();

        contract.Status.Should().Be(ContractStatus.Expired);
    }
}
