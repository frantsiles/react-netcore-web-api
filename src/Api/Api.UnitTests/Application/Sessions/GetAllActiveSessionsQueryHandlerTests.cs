using Api.Application.Sessions.Queries;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Sessions;

public class GetAllActiveSessionsQueryHandlerTests
{
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly GetAllActiveSessionsQueryHandler _handler;

    private static readonly DeviceInfo Device = DeviceInfo.Create("Chrome/120", "192.168.1.1");

    public GetAllActiveSessionsQueryHandlerTests()
    {
        _handler = new GetAllActiveSessionsQueryHandler(_sessionRepo.Object, _userRepo.Object);
    }

    [Fact]
    public async Task Handle_WithActiveSessions_ShouldReturnMappedAdminDtos()
    {
        var userId = Guid.NewGuid();
        var session = Session.Create(userId, "hash", Device, DateTime.UtcNow.AddDays(1));
        var user = User.Create("Alice", "Smith", "alice@example.com", "pw");

        _sessionRepo.Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync([session]);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _handler.Handle(new GetAllActiveSessionsQuery(), default);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(userId);
        result[0].UserEmail.Should().Be("alice@example.com");
        result[0].UserFullName.Should().Be(user.FullName);
        result[0].UserAgent.Should().Be("Chrome/120");
        result[0].IpAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnUnknownFallback()
    {
        var userId = Guid.NewGuid();
        var session = Session.Create(userId, "hash", Device, DateTime.UtcNow.AddDays(1));

        _sessionRepo.Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync([session]);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new GetAllActiveSessionsQuery(), default);

        result[0].UserEmail.Should().Be("unknown");
        result[0].UserFullName.Should().Be("Unknown User");
    }

    [Fact]
    public async Task Handle_MultipleSessionsSameUser_ShouldOnlyQueryUserOnce()
    {
        var userId = Guid.NewGuid();
        var s1 = Session.Create(userId, "hash1", Device, DateTime.UtcNow.AddDays(1));
        var s2 = Session.Create(userId, "hash2", Device, DateTime.UtcNow.AddDays(1));
        var user = User.Create("Bob", "Jones", "bob@example.com", "pw");

        _sessionRepo.Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync([s1, s2]);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _handler.Handle(new GetAllActiveSessionsQuery(), default);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.UserEmail.Should().Be("bob@example.com"));
        _userRepo.Verify(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoSessions_ShouldReturnEmptyList()
    {
        _sessionRepo.Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);

        var result = await _handler.Handle(new GetAllActiveSessionsQuery(), default);

        result.Should().BeEmpty();
        _userRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
