using Api.IntegrationTests.Common;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.IntegrationTests.Auth;

public class RefreshTokenTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(string Token, string RefreshToken)> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@demo.com", password = "Admin123!" });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (
            json.GetProperty("token").GetString()!,
            json.GetProperty("refreshToken").GetString()!
        );
    }

    [Fact]
    public async Task Login_ShouldReturnRefreshToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@demo.com", password = "Admin123!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("sessionId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ThenRefresh_ShouldReturnNewTokens()
    {
        var (_, refreshToken) = await LoginAsync();

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = "invalid-token-value" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_UsedTwice_ShouldReturn401OnSecondUse()
    {
        var (_, refreshToken) = await LoginAsync();

        var first = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken });
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
