using Api.IntegrationTests.Common;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messages;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.IntegrationTests.Users;

public class UsersCommandsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    private async Task<HttpClient> AuthenticatedAsync(string email, string password)
    {
        var token = await LoginAsync(email, password);
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    [Fact]
    public async Task CreateUser_AsAdmin_ShouldReturn201AndPublishEvent()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var client = await AuthenticatedAsync("admin@demo.com", "Admin123!");
        var email = $"new.user.{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            firstName = "New",
            lastName = "User",
            email,
            password = "Password123!",
            roleName = "Viewer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await harness.Published.Any<UserCreated>(x => x.Context.Message.Email == email))
            .Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ShouldReturn400()
    {
        var client = await AuthenticatedAsync("admin@demo.com", "Admin123!");

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            firstName = "Admin",
            lastName = "Demo",
            email = "admin@demo.com",
            password = "Password123!",
            roleName = "Viewer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_ShouldReturn204()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var admin = await AuthenticatedAsync("admin@demo.com", "Admin123!");

        var email = $"to.delete.{Guid.NewGuid():N}@example.com";
        var create = await admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Temp",
            lastName = "User",
            email,
            password = "Password123!",
            roleName = "Viewer"
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var del = await admin.DeleteAsync($"/api/users/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await harness.Published.Any<UserDeleted>(x => x.Context.Message.UserId == id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_AsNonAdmin_ShouldReturn403()
    {
        var viewer = await AuthenticatedAsync("user@demo.com", "User123!");
        var del = await viewer.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        del.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeUserRole_AsAdmin_ShouldReturn200()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var admin = await AuthenticatedAsync("admin@demo.com", "Admin123!");

        var email = $"role.change.{Guid.NewGuid():N}@example.com";
        var create = await admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Role",
            lastName = "Change",
            email,
            password = "Password123!",
            roleName = "Viewer"
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var patch = await admin.PatchAsync($"/api/users/{id}/role",
            JsonContent.Create(new { roleName = "Admin" }));
        patch.StatusCode.Should().Be(HttpStatusCode.OK);
        (await harness.Published.Any<UserRoleChanged>(x =>
            x.Context.Message.UserId == id &&
            x.Context.Message.OldRole == "Viewer" &&
            x.Context.Message.NewRole == "Admin")).Should().BeTrue();
    }

    [Fact]
    public async Task PostWithIdempotencyKey_CalledTwice_ShouldReturn201BothTimes()
    {
        var admin = await AuthenticatedAsync("admin@demo.com", "Admin123!");

        var email = $"idem.{Guid.NewGuid():N}@example.com";
        string idempotencyKey = Guid.NewGuid().ToString();
        var payload = new
        {
            firstName = "Idem",
            lastName = "Potent",
            email,
            password = "Password123!",
            roleName = "Viewer"
        };

        var first = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(payload)
        };
        first.Headers.Add("X-Idempotency-Key", idempotencyKey);
        var firstResponse = await admin.SendAsync(first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        string firstBody = await firstResponse.Content.ReadAsStringAsync();

        var second = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(payload)
        };
        second.Headers.Add("X-Idempotency-Key", idempotencyKey);
        var secondResponse = await admin.SendAsync(second);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        string secondBody = await secondResponse.Content.ReadAsStringAsync();

        secondBody.Should().Be(firstBody);
    }
}
