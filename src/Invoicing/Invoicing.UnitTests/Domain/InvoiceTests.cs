using Api.Domain.Common;
using FluentAssertions;
using Invoicing.Domain.Invoices;

namespace Invoicing.UnitTests.Domain;

public class InvoiceTests
{
    private static readonly Guid _customerId = Guid.NewGuid();
    private const string Currency = "USD";

    private static Invoice CreateDraftInvoice(string number = "INV-202506-AAA001") =>
        Invoice.Create(number, _customerId, Currency,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));

    private static InvoiceLine CreateLine(decimal amount = 100m) =>
        InvoiceLine.Create("Service item", 1, Money.Of(amount, Currency));

    [Fact]
    public void Create_ValidArgs_ReturnsDraftInvoice()
    {
        var inv = CreateDraftInvoice();
        inv.Status.Should().Be(InvoiceStatus.Draft);
        inv.TotalAmount.Amount.Should().Be(0);
        inv.BalanceDue.Amount.Should().Be(0);
        inv.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "InvoiceCreatedEvent");
    }

    [Fact]
    public void Create_EmptyNumber_ThrowsDomainException()
    {
        var act = () => Invoice.Create("", _customerId, Currency,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_DueDateBeforeIssueDate_ThrowsDomainException()
    {
        var act = () => Invoice.Create("INV-001", _customerId, Currency,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        act.Should().Throw<DomainException>().WithMessage("*DueDate*");
    }

    [Fact]
    public void AddLine_RecalculatesTotals()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine(200m));
        inv.Subtotal.Amount.Should().Be(200m);
        inv.TotalAmount.Amount.Should().Be(200m);
        inv.BalanceDue.Amount.Should().Be(200m);
    }

    [Fact]
    public void Issue_WithLines_MovesToIssued()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine(100m));
        inv.Issue();
        inv.Status.Should().Be(InvoiceStatus.Issued);
        inv.DomainEvents.Should().Contain(e => e.GetType().Name == "InvoiceIssuedEvent");
    }

    [Fact]
    public void Issue_WithNoLines_ThrowsDomainException()
    {
        var inv = CreateDraftInvoice();
        var act = () => inv.Issue();
        act.Should().Throw<DomainException>().WithMessage("*no lines*");
    }

    [Fact]
    public void Issue_AlreadyIssued_ThrowsDomainException()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine());
        inv.Issue();
        var act = () => inv.Issue();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordPayment_FullAmount_MarksPaid()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine(100m));
        inv.Issue();
        inv.RecordPayment(Money.Of(100m, Currency), DateOnly.FromDateTime(DateTime.UtcNow));
        inv.Status.Should().Be(InvoiceStatus.Paid);
        inv.PaidAmount.Amount.Should().Be(100m);
        inv.BalanceDue.Amount.Should().Be(0m);
    }

    [Fact]
    public void RecordPayment_PartialAmount_MarksPartiallyPaid()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine(100m));
        inv.Issue();
        inv.RecordPayment(Money.Of(40m, Currency), DateOnly.FromDateTime(DateTime.UtcNow));
        inv.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        inv.PaidAmount.Amount.Should().Be(40m);
        inv.BalanceDue.Amount.Should().Be(60m);
    }

    [Fact]
    public void RecordPayment_ExceedsBalance_ThrowsDomainException()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine(100m));
        inv.Issue();
        var act = () => inv.RecordPayment(Money.Of(150m, Currency), DateOnly.FromDateTime(DateTime.UtcNow));
        act.Should().Throw<DomainException>().WithMessage("*exceeds balance*");
    }

    [Fact]
    public void RecordPayment_WrongCurrency_ThrowsDomainException()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine(100m));
        inv.Issue();
        var act = () => inv.RecordPayment(Money.Of(50m, "EUR"), DateOnly.FromDateTime(DateTime.UtcNow));
        act.Should().Throw<DomainException>().WithMessage("*currency*");
    }

    [Fact]
    public void Cancel_IssuedInvoice_MovesCancelled()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine());
        inv.Issue();
        inv.Cancel("Customer request");
        inv.Status.Should().Be(InvoiceStatus.Cancelled);
        inv.DomainEvents.Should().Contain(e => e.GetType().Name == "InvoiceCancelledEvent");
    }

    [Fact]
    public void Cancel_PaidInvoice_ThrowsDomainException()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine(100m));
        inv.Issue();
        inv.RecordPayment(Money.Of(100m, Currency), DateOnly.FromDateTime(DateTime.UtcNow));
        var act = () => inv.Cancel("Too late");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddLine_WrongCurrency_ThrowsDomainException()
    {
        var inv = CreateDraftInvoice();
        var act = () => inv.AddLine(InvoiceLine.Create("Item", 1, Money.Of(50m, "EUR")));
        act.Should().Throw<DomainException>().WithMessage("*currency*");
    }

    [Fact]
    public void AddLine_ToIssuedInvoice_ThrowsDomainException()
    {
        var inv = CreateDraftInvoice();
        inv.AddLine(CreateLine());
        inv.Issue();
        var act = () => inv.AddLine(CreateLine());
        act.Should().Throw<DomainException>().WithMessage("*Issued*");
    }
}
