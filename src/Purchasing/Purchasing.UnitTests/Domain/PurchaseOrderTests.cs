using Api.Domain.Common;
using FluentAssertions;
using Purchasing.Domain.DomainEvents;
using Purchasing.Domain.PurchaseOrders;

namespace Purchasing.UnitTests.Domain;

public class PurchaseOrderTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static PurchaseOrder NewDraftPo() =>
        PurchaseOrder.Create(TenantId, "PO-202506-ABCDEF", Guid.NewGuid(), "USD", "US");

    private static PurchaseOrderLine ALine(Guid? catalogItemId = null) =>
        PurchaseOrderLine.Create(
            catalogItemId ?? Guid.NewGuid(), "SKU-001", "Widget", 10m,
            Money.Of(5m, "USD"));

    [Fact]
    public void Create_ValidArgs_SetsDraftStatus()
    {
        var po = NewDraftPo();

        po.Status.Should().Be(PurchaseOrderStatus.Draft);
        po.Lines.Should().BeEmpty();
        po.Subtotal.Amount.Should().Be(0);
    }

    [Fact]
    public void Create_RaisesPurchaseOrderCreatedEvent()
    {
        var po = NewDraftPo();

        po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderCreatedEvent>();
    }

    [Fact]
    public void AddLine_ValidLine_AddsAndRecalculates()
    {
        var po = NewDraftPo();
        var catalogItemId = Guid.NewGuid();

        po.AddLine(catalogItemId, "SKU-001", "Widget", 10m, Money.Of(5m, "USD"));

        po.Lines.Should().HaveCount(1);
        po.Subtotal.Amount.Should().Be(50m);
        po.Total.Amount.Should().Be(50m);
    }

    [Fact]
    public void AddLine_DuplicateCatalogItem_ThrowsDomainException()
    {
        var po = NewDraftPo();
        var catalogItemId = Guid.NewGuid();
        po.AddLine(catalogItemId, "SKU-001", "Widget", 10m, Money.Of(5m, "USD"));

        var act = () => po.AddLine(catalogItemId, "SKU-001", "Widget", 5m, Money.Of(3m, "USD"));

        act.Should().Throw<DomainException>().WithMessage("*already in*");
    }

    [Fact]
    public void AddLine_WrongCurrency_ThrowsDomainException()
    {
        var po = NewDraftPo();

        var act = () => po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 5m, Money.Of(3m, "EUR"));

        act.Should().Throw<DomainException>().WithMessage("*currency*");
    }

    [Fact]
    public void AddLine_NonDraftPo_ThrowsDomainException()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 5m, Money.Of(3m, "USD"));
        po.Send();
        po.ClearDomainEvents();

        var act = () => po.AddLine(Guid.NewGuid(), "SKU-002", "Gadget", 2m, Money.Of(10m, "USD"));

        act.Should().Throw<DomainException>().WithMessage("*edited*");
    }

    [Fact]
    public void RemoveLine_ExistingLine_RemovesAndRecalculates()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 10m, Money.Of(5m, "USD"));
        var lineId = po.Lines[0].Id;

        po.RemoveLine(lineId);

        po.Lines.Should().BeEmpty();
        po.Subtotal.Amount.Should().Be(0);
    }

    [Fact]
    public void Send_DraftWithLines_ChangesSentStatus()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 5m, Money.Of(10m, "USD"));
        po.ClearDomainEvents();

        po.Send();

        po.Status.Should().Be(PurchaseOrderStatus.Sent);
        po.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PurchaseOrderSentEvent>();
    }

    [Fact]
    public void Send_EmptyPo_ThrowsDomainException()
    {
        var po = NewDraftPo();

        var act = () => po.Send();

        act.Should().Throw<DomainException>().WithMessage("*no lines*");
    }

    [Fact]
    public void Confirm_SentPo_ConfirmsSuccessfully()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 5m, Money.Of(10m, "USD"));
        po.Send();
        po.ClearDomainEvents();

        po.Confirm();

        po.Status.Should().Be(PurchaseOrderStatus.Confirmed);
        po.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PurchaseOrderConfirmedEvent>();
    }

    [Fact]
    public void ReceiveLine_PartialQty_SetsPartiallyReceived()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 10m, Money.Of(5m, "USD"));
        po.Send();
        po.Confirm();
        po.ClearDomainEvents();
        var lineId = po.Lines[0].Id;

        po.ReceiveLine(lineId, 5m);

        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        po.Lines[0].QuantityReceived.Should().Be(5m);
        po.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PurchaseOrderLineReceivedEvent>();
    }

    [Fact]
    public void ReceiveLine_FullQty_SetsReceived()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 10m, Money.Of(5m, "USD"));
        po.Send();
        po.Confirm();
        var lineId = po.Lines[0].Id;

        po.ReceiveLine(lineId, 10m);

        po.Status.Should().Be(PurchaseOrderStatus.Received);
    }

    [Fact]
    public void ReceiveLine_ExceedsOrderedQty_ThrowsDomainException()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 5m, Money.Of(10m, "USD"));
        po.Send();
        po.Confirm();
        var lineId = po.Lines[0].Id;

        var act = () => po.ReceiveLine(lineId, 10m);

        act.Should().Throw<DomainException>().WithMessage("*more than ordered*");
    }

    [Fact]
    public void Cancel_ConfirmedPo_CancelledSuccessfully()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 5m, Money.Of(10m, "USD"));
        po.Send();
        po.Confirm();
        po.ClearDomainEvents();

        po.Cancel("Supplier unavailable");

        po.Status.Should().Be(PurchaseOrderStatus.Cancelled);
        po.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PurchaseOrderCancelledEvent>();
    }

    [Fact]
    public void Cancel_FullyReceivedPo_ThrowsDomainException()
    {
        var po = NewDraftPo();
        po.AddLine(Guid.NewGuid(), "SKU-001", "Widget", 5m, Money.Of(10m, "USD"));
        po.Send();
        po.Confirm();
        po.ReceiveLine(po.Lines[0].Id, 5m);

        var act = () => po.Cancel("reason");

        act.Should().Throw<DomainException>().WithMessage("*fully received*");
    }
}
