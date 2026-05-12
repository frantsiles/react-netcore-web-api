using Api.Application.Assistant.Plugins;
using Api.Application.Users.Commands.CreateUser;
using Api.Application.Users.Queries.GetUsers;
using FluentAssertions;
using MediatR;
using Moq;
using System.Text.Json;

namespace Api.UnitTests.Application.Assistant;

public class UserPluginTests
{
    private readonly Mock<ISender> _sender = new();

    [Fact]
    public async Task GetUsersAsync_ShouldReturnJsonString()
    {
        IReadOnlyList<UserDto> users =
        [
            new(Guid.NewGuid(), "Ana", "García", "ana@demo.com", true, ["Admin"]),
            new(Guid.NewGuid(), "Luis", "Pérez", "luis@demo.com", false, ["Viewer"]),
        ];
        _sender.Setup(s => s.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(users);

        UserPlugin plugin = new(_sender.Object);
        string result = await plugin.GetUsersAsync();

        result.Should().NotBeNullOrEmpty();
        using JsonDocument doc = JsonDocument.Parse(result);
        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldReturnConfirmation()
    {
        UserDto created = new(Guid.NewGuid(), "Carlos", "López", "carlos@demo.com", true, ["Viewer"]);
        _sender.Setup(s => s.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(created);

        UserPlugin plugin = new(_sender.Object);
        string result = await plugin.CreateUserAsync("Carlos", "López", "carlos@demo.com", "Passw0rd!", "Viewer");

        result.Should().Contain("success");
        result.Should().Contain("true");
    }

    [Fact]
    public async Task GetUserCountAsync_ShouldReturnActiveCount()
    {
        IReadOnlyList<UserDto> users =
        [
            new(Guid.NewGuid(), "Ana", "García", "ana@demo.com", true, ["Admin"]),
            new(Guid.NewGuid(), "Luis", "Pérez", "luis@demo.com", false, ["Viewer"]),
        ];
        _sender.Setup(s => s.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(users);

        UserPlugin plugin = new(_sender.Object);
        string result = await plugin.GetUserCountAsync();

        result.Should().Be("1");
    }
}
