using Api.Application.Assistant.Plugins;
using Api.Application.Sessions.Commands;
using Api.Application.Sessions.Queries;
using FluentAssertions;
using MediatR;
using Moq;
using System.Text.Json;

namespace Api.UnitTests.Application.Assistant;

public class SessionPluginTests
{
    private readonly Mock<ISender> _sender = new();

    [Fact]
    public async Task GetActiveSessionsAsync_ShouldReturnJsonString()
    {
        IReadOnlyList<SessionAdminDto> sessions =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), "ana@demo.com", "Ana García",
                "Chrome/120", "192.168.1.1", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow),
        ];
        _sender.Setup(s => s.Send(It.IsAny<GetAllActiveSessionsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(sessions);

        SessionPlugin plugin = new(_sender.Object);
        string result = await plugin.GetActiveSessionsAsync();

        result.Should().NotBeNullOrEmpty();
        using JsonDocument doc = JsonDocument.Parse(result);
        doc.RootElement.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task RevokeSessionAsync_WithValidGuids_ShouldReturnConfirmation()
    {
        string sessionId = Guid.NewGuid().ToString();
        string requesterId = Guid.NewGuid().ToString();

        _sender.Setup(s => s.Send(It.IsAny<RevokeSessionCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        SessionPlugin plugin = new(_sender.Object);
        string result = await plugin.RevokeSessionAsync(sessionId, requesterId);

        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain(sessionId);
    }

    [Fact]
    public async Task GetSessionCountAsync_ShouldReturnCount()
    {
        IReadOnlyList<SessionAdminDto> sessions =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), "a@demo.com", "User A", "Chrome", "10.0.0.1",
                DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "b@demo.com", "User B", "Firefox", "10.0.0.2",
                DateTime.UtcNow, DateTime.UtcNow),
        ];
        _sender.Setup(s => s.Send(It.IsAny<GetAllActiveSessionsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(sessions);

        SessionPlugin plugin = new(_sender.Object);
        string result = await plugin.GetSessionCountAsync();

        result.Should().Be("2");
    }
}
