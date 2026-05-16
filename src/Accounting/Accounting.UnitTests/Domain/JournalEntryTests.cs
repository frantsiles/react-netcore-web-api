using Accounting.Domain.DomainEvents;
using Accounting.Domain.JournalEntries;
using Api.Domain.Common;
using FluentAssertions;

namespace Accounting.UnitTests.Domain;

public class JournalEntryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CashAccountId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();

    private static JournalEntry NewDraftEntry() =>
        JournalEntry.Create(TenantId, "JE-202506-ABCDEF",
            new DateTime(2025, 6, 1), "Test entry");

    [Fact]
    public void Create_ValidArgs_SetsDraftStatus()
    {
        var entry = NewDraftEntry();

        entry.Status.Should().Be(EntryStatus.Draft);
        entry.Lines.Should().BeEmpty();
        entry.FiscalPeriod.Should().Be("2025-06");
    }

    [Fact]
    public void AddLine_TwoLines_ShowsCorrectTotals()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 500m);
        entry.AddLine(RevenueAccountId, "4000", "Revenue", EntrySide.Credit, 500m);

        entry.TotalDebits.Should().Be(500m);
        entry.TotalCredits.Should().Be(500m);
        entry.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public void IsBalanced_UnbalancedEntry_ReturnsFalse()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 500m);
        entry.AddLine(RevenueAccountId, "4000", "Revenue", EntrySide.Credit, 300m);

        entry.IsBalanced.Should().BeFalse();
    }

    [Fact]
    public void Post_BalancedEntry_ChangesStatusToPosted()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 200m);
        entry.AddLine(RevenueAccountId, "4000", "Revenue", EntrySide.Credit, 200m);

        var lines = entry.Post();

        entry.Status.Should().Be(EntryStatus.Posted);
        lines.Should().HaveCount(2);
        entry.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<JournalEntryPostedEvent>();
    }

    [Fact]
    public void Post_UnbalancedEntry_ThrowsDomainException()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 500m);
        entry.AddLine(RevenueAccountId, "4000", "Revenue", EntrySide.Credit, 300m);

        var act = () => entry.Post();

        act.Should().Throw<DomainException>().WithMessage("*not balanced*");
    }

    [Fact]
    public void Post_LessThan2Lines_ThrowsDomainException()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 100m);

        var act = () => entry.Post();

        act.Should().Throw<DomainException>().WithMessage("*at least 2 lines*");
    }

    [Fact]
    public void Post_EmptyEntry_ThrowsDomainException()
    {
        var entry = NewDraftEntry();

        var act = () => entry.Post();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddLine_PostedEntry_ThrowsDomainException()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 100m);
        entry.AddLine(RevenueAccountId, "4000", "Revenue", EntrySide.Credit, 100m);
        entry.Post();

        var act = () => entry.AddLine(Guid.NewGuid(), "9999", "Other", EntrySide.Debit, 50m);

        act.Should().Throw<DomainException>().WithMessage("*modified*");
    }

    [Fact]
    public void RemoveLine_ExistingLine_RemovesIt()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 100m);
        var lineId = entry.Lines[0].Id;

        entry.RemoveLine(lineId);

        entry.Lines.Should().BeEmpty();
    }

    [Fact]
    public void CreateReversal_PostedEntry_CreatesFlippedReversal()
    {
        var entry = NewDraftEntry();
        entry.AddLine(CashAccountId, "1000", "Cash", EntrySide.Debit, 300m);
        entry.AddLine(RevenueAccountId, "4000", "Revenue", EntrySide.Credit, 300m);
        entry.Post();
        entry.ClearDomainEvents();

        var reversal = entry.CreateReversal("JE-REV-202507-XXXXXX", new DateTime(2025, 7, 1));

        entry.Status.Should().Be(EntryStatus.Reversed);
        reversal.Status.Should().Be(EntryStatus.Draft);
        reversal.Lines[0].Side.Should().Be(EntrySide.Credit);   // flipped
        reversal.Lines[1].Side.Should().Be(EntrySide.Debit);    // flipped
        reversal.IsBalanced.Should().BeTrue();
        entry.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<JournalEntryReversedEvent>();
    }

    [Fact]
    public void CreateReversal_DraftEntry_ThrowsDomainException()
    {
        var entry = NewDraftEntry();

        var act = () => entry.CreateReversal("JE-REV-001", DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*Posted*");
    }
}
