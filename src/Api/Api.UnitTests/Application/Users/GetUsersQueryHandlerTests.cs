using Api.Application.Users.Queries.GetUsers;
using Api.Domain.Roles;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Users;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _handler = new GetUsersQueryHandler(_userRepo.Object);
    }

    [Fact]
    public async Task Handle_WithUsers_ShouldReturnMappedDtos()
    {
        var user1 = User.Create("Alice", "Smith", "alice@example.com", "hash1");
        var user2 = User.Create("Bob", "Jones", "bob@example.com", "hash2");
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([user1, user2]);

        var result = await _handler.Handle(new GetUsersQuery(), default);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(u => u.Email == "alice@example.com");
        result.Should().ContainSingle(u => u.Email == "bob@example.com");
    }

    [Fact]
    public async Task Handle_MapsAllDtoFields_Correctly()
    {
        var user = User.Create("Alice", "Smith", "alice@example.com", "hash");
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([user]);

        var result = await _handler.Handle(new GetUsersQuery(), default);

        var dto = result.Single();
        dto.Id.Should().Be(user.Id);
        dto.FirstName.Should().Be("Alice");
        dto.LastName.Should().Be("Smith");
        dto.Email.Should().Be("alice@example.com");
        dto.IsActive.Should().BeTrue();
        dto.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithRoles_ShouldIncludeRoleNames()
    {
        var user = User.Create("Admin", "User", "admin@example.com", "hash");
        user.AssignRole(Role.Create("Admin", "Administrator role"));
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([user]);

        var result = await _handler.Handle(new GetUsersQuery(), default);

        result.Single().Roles.Should().ContainSingle("Admin");
    }

    [Fact]
    public async Task Handle_WithEmptyRepository_ShouldReturnEmptyList()
    {
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);

        var result = await _handler.Handle(new GetUsersQuery(), default);

        result.Should().BeEmpty();
    }
}
