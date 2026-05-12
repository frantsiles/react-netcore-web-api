using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.ChangeUserRole;
using Api.Domain.Common;
using Api.Domain.Roles;
using Api.Domain.Roles.Repositories;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;
using Shared.Messages;

namespace Api.UnitTests.Application.Users;

public class ChangeUserRoleCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly ChangeUserRoleCommandHandler _handler;

    public ChangeUserRoleCommandHandlerTests()
    {
        _handler = new ChangeUserRoleCommandHandler(_userRepo.Object, _roleRepo.Object, _eventPublisher.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldChangeRoleAndPublishEvent()
    {
        var user = User.Create("Jane", "Smith", "jane@example.com", "hash");
        user.AssignRole(Role.Create("User", "default"));
        var adminRole = Role.Create("Admin", "admins");

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _roleRepo.Setup(r => r.GetByNameAsync("Admin", It.IsAny<CancellationToken>())).ReturnsAsync(adminRole);

        var dto = await _handler.Handle(new ChangeUserRoleCommand(user.Id, "Admin"), default);

        dto.Roles.Should().BeEquivalentTo(["Admin"]);
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<UserRoleChanged>(e => e.OldRole == "User" && e.NewRole == "Admin"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidUser_ShouldThrowDomainException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = async () => await _handler.Handle(new ChangeUserRoleCommand(Guid.NewGuid(), "Admin"), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*does not exist*");
    }

    [Fact]
    public async Task Handle_WithInvalidRole_ShouldThrowDomainException()
    {
        var user = User.Create("Jane", "Smith", "jane@example.com", "hash");
        user.AssignRole(Role.Create("User", "default"));

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _roleRepo.Setup(r => r.GetByNameAsync("Ghost", It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var act = async () => await _handler.Handle(new ChangeUserRoleCommand(user.Id, "Ghost"), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*does not exist*");
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<UserRoleChanged>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
