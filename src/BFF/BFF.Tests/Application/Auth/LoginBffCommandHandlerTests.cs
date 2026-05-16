using BFF.Application.Auth;
using BFF.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BFF.Tests.Application.Auth;

public class LoginBffCommandHandlerTests
{
    private readonly Mock<IApiClient> _apiClient = new();
    private readonly LoginBffCommandHandler _handler;

    public LoginBffCommandHandlerTests()
    {
        _handler = new LoginBffCommandHandler(_apiClient.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnMappedBffResult()
    {
        var sessionId = Guid.NewGuid();
        var permissions = new[] { "users:read", "users:write" };

        _apiClient
            .Setup(c => c.PostAsync<LoginRequest, LoginResponse>(
                "api/auth/login", It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse(
                "jwt-token", "refresh-token", sessionId,
                "admin@demo.com", "Admin User", permissions));

        var result = await _handler.Handle(
            new LoginBffCommand("admin@demo.com", "Admin123!", "TestAgent", "127.0.0.1"), default);

        result.Token.Should().Be("jwt-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.SessionId.Should().Be(sessionId);
        result.Email.Should().Be("admin@demo.com");
        result.FullName.Should().Be("Admin User");
        result.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task Handle_WhenApiReturnsNull_ShouldThrowInvalidOperationException()
    {
        _apiClient
            .Setup(c => c.PostAsync<LoginRequest, LoginResponse>(
                "api/auth/login", It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginResponse?)null);

        var act = async () => await _handler.Handle(
            new LoginBffCommand("admin@demo.com", "wrong", "Agent", "127.0.0.1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty response*");
    }

    [Fact]
    public async Task Handle_ShouldForwardAllCredentialsToApiClient()
    {
        var sessionId = Guid.NewGuid();
        _apiClient
            .Setup(c => c.PostAsync<LoginRequest, LoginResponse>(
                "api/auth/login", It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse("t", "rt", sessionId, "u@u.com", "U", []));

        await _handler.Handle(
            new LoginBffCommand("user@demo.com", "password", "Mozilla/5.0", "10.0.0.1"), default);

        _apiClient.Verify(c => c.PostAsync<LoginRequest, LoginResponse>(
            "api/auth/login",
            It.Is<LoginRequest>(r =>
                r.Email == "user@demo.com" &&
                r.Password == "password" &&
                r.UserAgent == "Mozilla/5.0" &&
                r.IpAddress == "10.0.0.1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
