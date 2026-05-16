using Api.Domain.Common;
using FluentAssertions;
using Moq;
using Parties.Application.Queries.GetPartyById;
using Parties.Domain.Parties;
using Parties.Domain.Repositories;

namespace Parties.UnitTests.Application;

public class GetPartyByIdQueryHandlerTests
{
    private readonly Mock<IPartyRepository> _repo = new();
    private readonly GetPartyByIdQueryHandler _handler;

    public GetPartyByIdQueryHandlerTests()
    {
        _handler = new GetPartyByIdQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_WhenPartyExists_ReturnsDto()
    {
        var party = Party.Create("Acme", PartyType.Organization, "US",
            PartyRole.Create(PartyRoleType.Customer));
        _repo.Setup(r => r.GetByIdAsync(party.Id, default)).ReturnsAsync(party);

        var dto = await _handler.Handle(new GetPartyByIdQuery(party.Id), default);

        dto.PartyId.Should().Be(party.Id);
        dto.LegalName.Should().Be("Acme");
    }

    [Fact]
    public async Task Handle_WhenPartyNotFound_ThrowsDomainException()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Party?)null);

        var act = async () => await _handler.Handle(new GetPartyByIdQuery(id), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage($"*{id}*not found*");
    }
}
