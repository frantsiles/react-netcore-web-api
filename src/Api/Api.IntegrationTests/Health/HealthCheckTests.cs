using Api.IntegrationTests.Common;
using FluentAssertions;

namespace Api.IntegrationTests.Health;

/// <summary>
/// Integration test: verifies the health endpoint responds correctly
/// using the full in-process pipeline (middleware, routing, controllers).
/// </summary>
public class HealthCheckTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Get_HealthEndpoint_ShouldReturn200()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
