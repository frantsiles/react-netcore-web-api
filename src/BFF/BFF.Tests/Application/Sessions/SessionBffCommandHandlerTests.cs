using BFF.Application.Sessions;
using BFF.Domain.Interfaces;
using Moq;

namespace BFF.Tests.Application.Sessions;

public class SessionBffCommandHandlerTests
{
    private readonly Mock<IApiClient> _apiClient = new();

    public SessionBffCommandHandlerTests()
    {
        _apiClient.Setup(c => c.PatchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _apiClient.Setup(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task RevokeSessionBffCommand_ShouldCallCorrectEndpointWithToken()
    {
        var sessionId = Guid.NewGuid();
        var handler = new RevokeSessionBffCommandHandler(_apiClient.Object);

        await handler.Handle(new RevokeSessionBffCommand(sessionId, "bearer-token"), default);

        _apiClient.Verify(c => c.PatchAsync(
            $"api/sessions/{sessionId}/revoke",
            "bearer-token",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAllMySessionsBffCommand_ShouldCallCorrectEndpointWithToken()
    {
        var handler = new RevokeAllMySessionsBffCommandHandler(_apiClient.Object);

        await handler.Handle(new RevokeAllMySessionsBffCommand("bearer-token"), default);

        _apiClient.Verify(c => c.DeleteAsync(
            "api/sessions/my",
            "bearer-token",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAllUserSessionsBffCommand_ShouldCallCorrectEndpointWithUserId()
    {
        var userId = Guid.NewGuid();
        var handler = new RevokeAllUserSessionsBffCommandHandler(_apiClient.Object);

        await handler.Handle(new RevokeAllUserSessionsBffCommand(userId, "admin-token"), default);

        _apiClient.Verify(c => c.DeleteAsync(
            $"api/admin/users/{userId}/sessions",
            "admin-token",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
