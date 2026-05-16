using BFF.Application.Sessions;
using BFF.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BFF.Tests.Application.Sessions;

public class SessionBffQueryHandlerTests
{
    private readonly Mock<IApiClient> _apiClient = new();

    // ── GetMySessionsBffQuery ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMySessionsBffQuery_ShouldForwardTokenAndReturnSessions()
    {
        var sessions = new List<SessionBffDto>
        {
            new(Guid.NewGuid(), "Chrome/120", "127.0.0.1",
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, DateTime.UtcNow.AddDays(29),
                IsActive: true, IsCurrent: true)
        };

        _apiClient
            .Setup(c => c.GetAsync<List<SessionBffDto>>("api/sessions/my", "bearer-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var handler = new GetMySessionsBffQueryHandler(_apiClient.Object);
        var result = await handler.Handle(new GetMySessionsBffQuery("bearer-token"), default);

        result.Should().HaveCount(1);
        result[0].IsCurrent.Should().BeTrue();
        _apiClient.Verify(c => c.GetAsync<List<SessionBffDto>>("api/sessions/my", "bearer-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMySessionsBffQuery_WhenApiReturnsNull_ShouldReturnEmptyList()
    {
        _apiClient
            .Setup(c => c.GetAsync<List<SessionBffDto>>("api/sessions/my", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<SessionBffDto>?)null);

        var handler = new GetMySessionsBffQueryHandler(_apiClient.Object);
        var result = await handler.Handle(new GetMySessionsBffQuery("token"), default);

        result.Should().BeEmpty();
    }

    // ── GetAllSessionsBffQuery (Admin) ─────────────────────────────────────────

    [Fact]
    public async Task GetAllSessionsBffQuery_ShouldForwardTokenAndReturnAdminDtos()
    {
        var sessions = new List<SessionAdminBffDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "admin@demo.com", "Admin User",
                "Firefox/121", "10.0.0.1", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow)
        };

        _apiClient
            .Setup(c => c.GetAsync<List<SessionAdminBffDto>>("api/sessions", "admin-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var handler = new GetAllSessionsBffQueryHandler(_apiClient.Object);
        var result = await handler.Handle(new GetAllSessionsBffQuery("admin-token"), default);

        result.Should().HaveCount(1);
        result[0].UserEmail.Should().Be("admin@demo.com");
        _apiClient.Verify(c => c.GetAsync<List<SessionAdminBffDto>>("api/sessions", "admin-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllSessionsBffQuery_WhenApiReturnsNull_ShouldReturnEmptyList()
    {
        _apiClient
            .Setup(c => c.GetAsync<List<SessionAdminBffDto>>("api/sessions", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<SessionAdminBffDto>?)null);

        var handler = new GetAllSessionsBffQueryHandler(_apiClient.Object);
        var result = await handler.Handle(new GetAllSessionsBffQuery("token"), default);

        result.Should().BeEmpty();
    }
}
