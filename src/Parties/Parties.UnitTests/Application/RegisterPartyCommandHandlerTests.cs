using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentAssertions;
using Moq;
using Parties.Application.Commands.RegisterParty;
using Parties.Domain.Parties;
using Parties.Domain.Repositories;

namespace Parties.UnitTests.Application;

public class RegisterPartyCommandHandlerTests
{
    private readonly Mock<IPartyRepository> _repo = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly RegisterPartyCommandHandler _handler;

    public RegisterPartyCommandHandlerTests()
    {
        _handler = new RegisterPartyCommandHandler(_repo.Object, _events.Object);
    }

    private static RegisterPartyCommand ValidCommand(string? taxId = null) => new(
        "Acme Corp", null, PartyType.Organization, "US",
        PartyRoleType.Customer, taxId, null, null, null, null);

    [Fact]
    public async Task Handle_WithValidData_CreatesPartyAndPublishesEvent()
    {
        _repo.Setup(r => r.ExistsByTaxIdAsync(It.IsAny<string>(), It.IsAny<string>(), default))
             .ReturnsAsync(false);

        var dto = await _handler.Handle(ValidCommand(), default);

        dto.LegalName.Should().Be("Acme Corp");
        dto.PartyType.Should().Be("Organization");
        dto.IsActive.Should().BeTrue();
        dto.Roles.Should().HaveCount(1);
        _repo.Verify(r => r.AddAsync(It.IsAny<Party>(), default), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<object>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateTaxId_ThrowsDomainException()
    {
        _repo.Setup(r => r.ExistsByTaxIdAsync("12-3456789", "US", default))
             .ReturnsAsync(true);

        var act = async () => await _handler.Handle(ValidCommand("12-3456789"), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already exists*");
        _repo.Verify(r => r.AddAsync(It.IsAny<Party>(), default), Times.Never);
    }
}
