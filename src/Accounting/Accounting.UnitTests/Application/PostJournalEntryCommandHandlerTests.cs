using Accounting.Application.JournalEntries.Commands.PostJournalEntry;
using Accounting.Domain.Accounts;
using Accounting.Domain.JournalEntries;
using Accounting.Domain.Repositories;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentAssertions;
using Moq;

namespace Accounting.UnitTests.Application;

public class PostJournalEntryCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly Mock<IJournalEntryRepository> _jeRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly PostJournalEntryCommandHandler _sut;

    public PostJournalEntryCommandHandlerTests()
    {
        _sut = new PostJournalEntryCommandHandler(_jeRepo.Object, _accountRepo.Object, _events.Object);
    }

    [Fact]
    public async Task Handle_BalancedEntry_PostsAndUpdatesAccountBalances()
    {
        var cashAccount = Account.Create(TenantId, "1000", "Cash", AccountType.Asset, "USD");
        var revenueAccount = Account.Create(TenantId, "4000", "Revenue", AccountType.Revenue, "USD");

        var entry = JournalEntry.Create(TenantId, "JE-001", DateTime.UtcNow, "Sale");
        entry.AddLine(cashAccount.Id, "1000", "Cash", EntrySide.Debit, 500m);
        entry.AddLine(revenueAccount.Id, "4000", "Revenue", EntrySide.Credit, 500m);

        _jeRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        _accountRepo.Setup(r => r.GetByIdAsync(cashAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cashAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(revenueAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(revenueAccount);

        var result = await _sut.Handle(new PostJournalEntryCommand(entry.Id), CancellationToken.None);

        result.Status.Should().Be(EntryStatus.Posted);
        cashAccount.Balance.Should().Be(500m);
        revenueAccount.Balance.Should().Be(500m);
        _accountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_EntryNotFound_ThrowsDomainException()
    {
        _jeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((JournalEntry?)null);

        var act = async () => await _sut.Handle(
            new PostJournalEntryCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not found*");
    }
}
