using Accounting.Domain.Accounts;
using Accounting.Domain.DomainEvents;
using Api.Domain.Common;
using FluentAssertions;

namespace Accounting.UnitTests.Domain;

public class AccountTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArgs_SetsActiveAccount()
    {
        var account = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");

        account.AccountNumber.Should().Be("1000");
        account.Name.Should().Be("Cash");
        account.Type.Should().Be(AccountType.Asset);
        account.Balance.Should().Be(0m);
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_RaisesAccountCreatedEvent()
    {
        var account = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");

        account.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<AccountCreatedEvent>();
    }

    [Theory]
    [InlineData(AccountType.Asset, true)]
    [InlineData(AccountType.Expense, true)]
    [InlineData(AccountType.ContraRevenue, true)]
    [InlineData(AccountType.Liability, false)]
    [InlineData(AccountType.Equity, false)]
    [InlineData(AccountType.Revenue, false)]
    public void NormalBalanceIsDebit_CorrectForEachType(AccountType type, bool expected)
    {
        type.NormalBalanceIsDebit().Should().Be(expected);
    }

    [Fact]
    public void ApplyDebit_AssetAccount_IncreasesBalance()
    {
        var account = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");

        account.ApplyDebit(100m);

        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void ApplyCredit_AssetAccount_DecreasesBalance()
    {
        var account = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");
        account.ApplyDebit(200m);

        account.ApplyCredit(50m);

        account.Balance.Should().Be(150m);
    }

    [Fact]
    public void ApplyCredit_RevenueAccount_IncreasesBalance()
    {
        var account = Account.Create(TenantId, "4000", "Sales Revenue", AccountType.Revenue, "USD");

        account.ApplyCredit(500m);

        account.Balance.Should().Be(500m);
    }

    [Fact]
    public void ApplyDebit_RevenueAccount_DecreasesBalance()
    {
        var account = Account.Create(TenantId, "4000", "Sales Revenue", AccountType.Revenue, "USD");
        account.ApplyCredit(500m);

        account.ApplyDebit(100m);

        account.Balance.Should().Be(400m);
    }

    [Fact]
    public void ApplyDebit_ZeroAmount_ThrowsDomainException()
    {
        var account = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");

        var act = () => account.ApplyDebit(0m);

        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void Deactivate_ActiveAccount_DeactivatesIt()
    {
        var account = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsDomainException()
    {
        var account = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");
        account.Deactivate();

        var act = () => account.Deactivate();

        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }
}
