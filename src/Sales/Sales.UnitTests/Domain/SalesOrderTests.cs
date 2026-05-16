using Api.Domain.Common;
using FluentAssertions;
using Sales.Domain.DomainEvents;
using Sales.Domain.Orders;

namespace Sales.UnitTests.Domain;

public class SalesOrderTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static SalesOrder AnOrder(Guid? originQuoteId = null) =>
        SalesOrder.Create("SO-202506-AABBCC", Guid.NewGuid(), Today, "USD", "US",
            null, originQuoteId);

    private static SalesOrderLine ALine(string sku = "SKU-001") =>
        SalesOrderLine.Create(Guid.NewGuid(), sku, "Widget", 3, Money.Of(15m, "USD"));

    [Fact]
    public void Create_WithValidData_CreatesDraftOrder()
    {
        var o = AnOrder();

        o.Status.Should().Be(SalesOrderStatus.Draft);
        o.Lines.Should().BeEmpty();
        o.Subtotal.Amount.Should().Be(0);
        o.OriginQuoteId.Should().BeNull();
    }

    [Fact]
    public void Create_WithOriginQuoteId_StoresReference()
    {
        var quoteId = Guid.NewGuid();
        var o = AnOrder(originQuoteId: quoteId);

        o.OriginQuoteId.Should().Be(quoteId);
    }

    [Fact]
    public void Create_RaisesSalesOrderCreatedEvent()
    {
        var o = AnOrder();
        o.DomainEvents[0].Should().BeOfType<SalesOrderCreatedEvent>();
    }

    [Fact]
    public void Create_WithDeliveryBeforeOrder_ThrowsDomainException()
    {
        var yesterday = Today.AddDays(-1);
        var act = () => SalesOrder.Create("SO-001", Guid.NewGuid(), Today, "USD", "US", yesterday);
        act.Should().Throw<DomainException>().WithMessage("*RequestedDeliveryDate*");
    }

    [Fact]
    public void AddLine_UpdatesTotals()
    {
        var o = AnOrder();
        o.AddLine(ALine());

        o.Lines.Should().HaveCount(1);
        o.Subtotal.Amount.Should().Be(45m); // 3 * 15
    }

    [Fact]
    public void AddLine_DuplicateCatalogItem_ThrowsDomainException()
    {
        var o = AnOrder();
        var catalogItemId = Guid.NewGuid();
        o.AddLine(SalesOrderLine.Create(catalogItemId, "SKU-001", "Widget", 1, Money.Of(15m, "USD")));

        var act = () => o.AddLine(SalesOrderLine.Create(catalogItemId, "SKU-001", "Widget", 2, Money.Of(15m, "USD")));
        act.Should().Throw<DomainException>().WithMessage("*already on this order*");
    }

    [Fact]
    public void RemoveLine_ExistingLine_RecalculatesTotals()
    {
        var o = AnOrder();
        var line = ALine();
        o.AddLine(line);

        o.RemoveLine(line.Id);

        o.Lines.Should().BeEmpty();
        o.Subtotal.Amount.Should().Be(0);
    }

    [Fact]
    public void Confirm_WithLines_TransitionsToConfirmed_AndRaisesEvent()
    {
        var o = AnOrder();
        o.AddLine(ALine());
        o.ClearDomainEvents();

        o.Confirm();

        o.Status.Should().Be(SalesOrderStatus.Confirmed);
        o.DomainEvents[0].Should().BeOfType<SalesOrderConfirmedEvent>();
    }

    [Fact]
    public void Confirm_WithNoLines_ThrowsDomainException()
    {
        var o = AnOrder();
        var act = () => o.Confirm();
        act.Should().Throw<DomainException>().WithMessage("*no lines*");
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_ThrowsDomainException()
    {
        var o = AnOrder();
        o.AddLine(ALine());
        o.Confirm();

        var act = () => o.Confirm();
        act.Should().Throw<DomainException>().WithMessage("*Draft*");
    }

    [Fact]
    public void Cancel_DraftOrder_TransitionsToCancelled_AndRaisesEvent()
    {
        var o = AnOrder();
        o.ClearDomainEvents();

        o.Cancel("Customer requested cancellation");

        o.Status.Should().Be(SalesOrderStatus.Cancelled);
        o.DomainEvents[0].Should().BeOfType<SalesOrderCancelledEvent>();
    }

    [Fact]
    public void Cancel_FulfilledOrder_ThrowsDomainException()
    {
        var o = AnOrder();
        var act = () =>
        {
            // Force status to Fulfilled by reflection for this edge case test
            typeof(SalesOrder)
                .GetProperty(nameof(SalesOrder.Status))!
                .SetValue(o, SalesOrderStatus.Fulfilled);
            o.Cancel("attempt");
        };
        act.Should().Throw<DomainException>().WithMessage("*Fulfilled*");
    }

    [Fact]
    public void AddLine_OnConfirmedOrder_ThrowsDomainException()
    {
        var o = AnOrder();
        o.AddLine(ALine());
        o.Confirm();

        var act = () => o.AddLine(SalesOrderLine.Create(Guid.NewGuid(), "SKU-002", "Other", 1, Money.Of(5m, "USD")));
        act.Should().Throw<DomainException>().WithMessage("*Confirmed*");
    }
}
