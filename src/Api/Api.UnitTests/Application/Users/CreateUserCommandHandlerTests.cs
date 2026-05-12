using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.CreateUser;
using Api.Domain.Common;
using Api.Domain.Roles;
using Api.Domain.Roles.Repositories;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;
using Shared.Messages;

namespace Api.UnitTests.Application.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashedPw");
        _handler = new CreateUserCommandHandler(
            _userRepo.Object,
            _roleRepo.Object,
            _passwordHasher.Object,
            _eventPublisher.Object);
    }

    private static CreateUserCommand ValidCommand()
        => new("John", "Doe", "john@example.com", "Password123", "User");

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateUserAndPublishEvent()
    {
        var role = Role.Create("User", "Default role");
        _userRepo.Setup(r => r.ExistsByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _roleRepo.Setup(r => r.GetByNameAsync("User", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(role);

        var dto = await _handler.Handle(ValidCommand(), default);

        dto.Email.Should().Be("john@example.com");
        dto.Roles.Should().Contain("User");
        _userRepo.Verify(r => r.AddAsync(It.IsAny<Api.Domain.Users.User>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<UserCreated>(e => e.Email == "john@example.com" && e.Role == "User"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldThrowDomainException()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = async () => await _handler.Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidRole_ShouldThrowDomainException()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _roleRepo.Setup(r => r.GetByNameAsync("User", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Role?)null);

        var act = async () => await _handler.Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*does not exist*");
    }
}
