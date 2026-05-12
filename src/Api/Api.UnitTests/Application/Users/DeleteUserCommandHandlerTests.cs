using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.DeleteUser;
using Api.Domain.Common;
using Api.Domain.Roles;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;
using Shared.Messages;

namespace Api.UnitTests.Application.Users;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _handler = new DeleteUserCommandHandler(_userRepo.Object, _eventPublisher.Object);
    }

    private static User MakeUser(string role)
    {
        var user = User.Create("Jane", "Smith", $"jane.{Guid.NewGuid():N}@example.com", "hash");
        user.AssignRole(Role.Create(role, $"{role} role"));
        return user;
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldDeactivateUserAndPublishEvent()
    {
        var user = MakeUser("User");
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await _handler.Handle(new DeleteUserCommand(user.Id), default);

        user.IsActive.Should().BeFalse();
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<UserDeleted>(e => e.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidId_ShouldThrowDomainException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = async () => await _handler.Handle(new DeleteUserCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*does not exist*");
    }

    [Fact]
    public async Task Handle_WithLastAdmin_ShouldThrowDomainException()
    {
        var admin = MakeUser("Admin");
        _userRepo.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([admin]);

        var act = async () => await _handler.Handle(new DeleteUserCommand(admin.Id), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*last active Admin*");
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<UserDeleted>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
