using Api.Domain.Common;
using Banking.Domain.BankAccounts;
using Banking.Domain.DomainEvents;
using FluentAssertions;

namespace Banking.UnitTests.Domain;

public class BankAccountTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static BankAccount NewAccount() =>
        BankAccount.Create(TenantId, "ACC-001", "First National Bank", "USD");

    [Fact]
    public void Create_ValidArgs_SetsActiveWithZeroBalance()
    {
        var account = NewAccount();

        account.AccountNumber.Should().Be("ACC-001");
        account.Status.Should().Be(BankAccountStatus.Active);
        account.Balance.Should().Be(0m);
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Create_RaisesBankAccountCreatedEvent()
    {
        var account = NewAccount();

        account.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BankAccountCreatedEvent>();
    }

    [Fact]
    public void AddTransaction_Credit_IncreasesBalance()
    {
        var account = NewAccount();

        account.AddTransaction(DateTime.Today, "Customer payment", 1000m, BankTransactionType.Credit);

        account.Balance.Should().Be(1000m);
        account.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void AddTransaction_Debit_DecreasesBalance()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Deposit", 500m, BankTransactionType.Credit);

        account.AddTransaction(DateTime.Today, "Supplier payment", 200m, BankTransactionType.Debit);

        account.Balance.Should().Be(300m);
    }

    [Fact]
    public void AddTransaction_MultipleTransactions_CorrectRunningBalance()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Deposit", 1000m, BankTransactionType.Credit);
        account.AddTransaction(DateTime.Today, "Expense", 150m, BankTransactionType.Debit);
        account.AddTransaction(DateTime.Today, "Income", 250m, BankTransactionType.Credit);

        account.Balance.Should().Be(1100m);
    }

    [Fact]
    public void ReconcileTransaction_Unreconciled_ReconcilesSetsJournalEntryId()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Payment", 500m, BankTransactionType.Credit);
        var txId = account.Transactions[0].Id;
        var journalEntryId = Guid.NewGuid();

        account.ReconcileTransaction(txId, journalEntryId);

        account.Transactions[0].Status.Should().Be(BankTransactionStatus.Reconciled);
        account.Transactions[0].LinkedJournalEntryId.Should().Be(journalEntryId);
    }

    [Fact]
    public void ReconcileTransaction_AlreadyReconciled_ThrowsDomainException()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Payment", 500m, BankTransactionType.Credit);
        var txId = account.Transactions[0].Id;
        account.ReconcileTransaction(txId, Guid.NewGuid());

        var act = () => account.ReconcileTransaction(txId, Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*reconciled*");
    }

    [Fact]
    public void VoidTransaction_Unreconciled_VoidsAndRevertsBalance()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Deposit", 500m, BankTransactionType.Credit);
        var txId = account.Transactions[0].Id;

        account.VoidTransaction(txId);

        account.Transactions[0].Status.Should().Be(BankTransactionStatus.Voided);
        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void VoidTransaction_Reconciled_ThrowsDomainException()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Payment", 500m, BankTransactionType.Credit);
        var txId = account.Transactions[0].Id;
        account.ReconcileTransaction(txId, Guid.NewGuid());

        var act = () => account.VoidTransaction(txId);

        act.Should().Throw<DomainException>().WithMessage("*reconciled transaction*");
    }

    [Fact]
    public void Close_WithUnreconciled_ThrowsDomainException()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Payment", 500m, BankTransactionType.Credit);

        var act = () => account.Close();

        act.Should().Throw<DomainException>().WithMessage("*unreconciled*");
    }

    [Fact]
    public void Close_AllReconciled_ClosesAccount()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Payment", 500m, BankTransactionType.Credit);
        account.ReconcileTransaction(account.Transactions[0].Id, Guid.NewGuid());

        account.Close();

        account.Status.Should().Be(BankAccountStatus.Closed);
    }

    [Fact]
    public void AddTransaction_ClosedAccount_ThrowsDomainException()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Payment", 100m, BankTransactionType.Credit);
        account.ReconcileTransaction(account.Transactions[0].Id, Guid.NewGuid());
        account.Close();

        var act = () => account.AddTransaction(DateTime.Today, "New tx", 50m, BankTransactionType.Credit);

        act.Should().Throw<DomainException>().WithMessage("*closed*");
    }

    [Fact]
    public void UnreconcileTransaction_Reconciled_ClearsJournalEntryId()
    {
        var account = NewAccount();
        account.AddTransaction(DateTime.Today, "Payment", 500m, BankTransactionType.Credit);
        var txId = account.Transactions[0].Id;
        account.ReconcileTransaction(txId, Guid.NewGuid());

        account.UnreconcileTransaction(txId);

        account.Transactions[0].Status.Should().Be(BankTransactionStatus.Unreconciled);
        account.Transactions[0].LinkedJournalEntryId.Should().BeNull();
    }
}
