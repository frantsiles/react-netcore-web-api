using Api.IntegrationTests.Common;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.IntegrationTests.Sessions;

public class SessionsControllerTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(string Token, string RefreshToken)> LoginAsync(
        string email = "admin@demo.com", string password = "Admin123!")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (
            json.GetProperty("token").GetString()!,
            json.GetProperty("refreshToken").GetString()!
        );
    }

    [Fact]
    public async Task GetMySessions_ShouldReturnActiveSessions()
    {
        var (token, _) = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/sessions/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllSessions_AsAdmin_ShouldReturnSessions()
    {
        var (token, _) = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllSessions_AsUser_ShouldReturn403()
    {
        var (token, _) = await LoginAsync("user@demo.com", "User123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeSession_AsAdmin_ShouldRevokeSession()
    {
        var (adminToken, _) = await LoginAsync();
        var (userToken, _) = await LoginAsync("user@demo.com", "User123!");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var sessionsResponse = await _client.GetAsync("/api/sessions/my");
        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = sessions[0].GetProperty("sessionId").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var revokeResponse = await _client.PatchAsync($"/api/sessions/{sessionId}/revoke", null);

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
