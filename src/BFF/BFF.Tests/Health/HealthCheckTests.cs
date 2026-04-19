using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BFF.Tests.Health;

public class HealthCheckTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_HealthEndpoint_ShouldReturn200()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/bff/health");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
