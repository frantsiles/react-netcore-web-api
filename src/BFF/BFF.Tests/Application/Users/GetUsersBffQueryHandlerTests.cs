using BFF.Application.Users;
using BFF.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BFF.Tests.Application.Users;

public class GetUsersBffQueryHandlerTests
{
    private readonly Mock<IApiClient> _apiClient = new();
    private readonly GetUsersBffQueryHandler _handler;

    public GetUsersBffQueryHandlerTests()
    {
        _handler = new GetUsersBffQueryHandler(_apiClient.Object);
    }

    [Fact]
    public async Task Handle_ShouldForwardBearerTokenAndReturnUsers()
    {
        var users = new List<UserBffDto>
        {
            new(Guid.NewGuid(), "Alice", "Smith", "alice@example.com", true, ["Admin"]),
            new(Guid.NewGuid(), "Bob", "Jones", "bob@example.com", true, ["Viewer"])
        };

        _apiClient
            .Setup(c => c.GetAsync<List<UserBffDto>>("api/users", "my-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _handler.Handle(new GetUsersBffQuery("my-token"), default);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(u => u.Email == "alice@example.com");
        _apiClient.Verify(c => c.GetAsync<List<UserBffDto>>("api/users", "my-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApiReturnsNull_ShouldReturnEmptyList()
    {
        _apiClient
            .Setup(c => c.GetAsync<List<UserBffDto>>("api/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<UserBffDto>?)null);

        var result = await _handler.Handle(new GetUsersBffQuery("token"), default);

        result.Should().BeEmpty();
    }
}
