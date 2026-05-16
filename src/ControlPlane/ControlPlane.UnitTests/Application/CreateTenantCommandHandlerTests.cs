using Api.Application.Common.Interfaces;
using ControlPlane.Application.Tenants.Commands.CreateTenant;
using ControlPlane.Domain.Repositories;
using ControlPlane.Domain.Tenants;
using FluentAssertions;
using Moq;

namespace ControlPlane.UnitTests.Application;

public class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _repo = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly CreateTenantCommandHandler _sut;

    public CreateTenantCommandHandlerTests()
    {
        _sut = new CreateTenantCommandHandler(_repo.Object, _events.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTenant()
    {
        _repo.Setup(r => r.ExistsBySlugAsync("acme", It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        var cmd = new CreateTenantCommand("Acme Corp", "acme", "US", "USD", TenantPlan.Standard);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Name.Should().Be("Acme Corp");
        result.Slug.Should().Be("acme");
        result.Plan.Should().Be(TenantPlan.Standard);
        _repo.Verify(r => r.AddAsync(It.IsAny<ControlPlane.Domain.Tenants.Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ThrowsDomainException()
    {
        _repo.Setup(r => r.ExistsBySlugAsync("existing", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var cmd = new CreateTenantCommand("Test", "existing", "US", "USD");

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<Api.Domain.Common.DomainException>()
            .WithMessage("*already taken*");
    }

    [Fact]
    public async Task Handle_PublishesDomainEvents()
    {
        _repo.Setup(r => r.ExistsBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        var cmd = new CreateTenantCommand("Acme", "acme", "US", "USD");
        await _sut.Handle(cmd, CancellationToken.None);

        _events.Verify(e => e.PublishAsync(
            It.IsAny<Api.Domain.Common.IDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
