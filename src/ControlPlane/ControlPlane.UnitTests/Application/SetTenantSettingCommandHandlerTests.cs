using ControlPlane.Application.Common.Interfaces;
using ControlPlane.Application.Tenants.Commands.SetTenantSetting;
using ControlPlane.Domain.Repositories;
using ControlPlane.Domain.Tenants;
using FluentAssertions;
using Moq;

namespace ControlPlane.UnitTests.Application;

public class SetTenantSettingCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _repo = new();
    private readonly Mock<ITenantSettingsCache> _cache = new();
    private readonly SetTenantSettingCommandHandler _sut;

    public SetTenantSettingCommandHandlerTests()
    {
        _sut = new SetTenantSettingCommandHandler(_repo.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_ValidSetting_SetsSetting()
    {
        var tenant = Tenant.Create("Acme", "acme", "US", "USD");
        _repo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(tenant);

        var cmd = new SetTenantSettingCommand(tenant.Id, "max_users", "500");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Settings.Should().ContainSingle(s => s.Key == "max_users" && s.Value == "500");
        _cache.Verify(c => c.Invalidate(tenant.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_TenantNotFound_ThrowsDomainException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((ControlPlane.Domain.Tenants.Tenant?)null);

        var cmd = new SetTenantSettingCommand(Guid.NewGuid(), "key", "value");

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<Api.Domain.Common.DomainException>()
            .WithMessage("*not found*");
    }
}
