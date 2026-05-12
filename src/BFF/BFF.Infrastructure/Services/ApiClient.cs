using BFF.Domain.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BFF.Infrastructure.Services;

/// <summary>
/// HttpClient-based implementation of IApiClient.
/// Named HttpClient "BackendApi" is registered in DependencyInjection.cs with the base URL.
/// </summary>
public class ApiClient(IHttpClientFactory httpClientFactory) : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path, TRequest body, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("BackendApi");
        var response = await client.PostAsync(path, Serialize(body), ct);
        response.EnsureSuccessStatusCode();
        return await Deserialize<TResponse>(response, ct);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path, TRequest body, string bearerToken, CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(bearerToken);
        var response = await client.PostAsync(path, Serialize(body), ct);
        response.EnsureSuccessStatusCode();
        return await Deserialize<TResponse>(response, ct);
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string path, string? bearerToken = null, CancellationToken ct = default)
    {
        var client = bearerToken is not null
            ? CreateAuthorizedClient(bearerToken)
            : httpClientFactory.CreateClient("BackendApi");

        var response = await client.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await Deserialize<TResponse>(response, ct);
    }

    public async Task PatchAsync(string path, string bearerToken, CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(bearerToken);
        var response = await client.PatchAsync(path, new StringContent(""), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(
        string path, TRequest body, string bearerToken, CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(bearerToken);
        var response = await client.PatchAsync(path, Serialize(body), ct);
        response.EnsureSuccessStatusCode();
        return await Deserialize<TResponse>(response, ct);
    }

    public async Task DeleteAsync(string path, string bearerToken, CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(bearerToken);
        var response = await client.DeleteAsync(path, ct);
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateAuthorizedClient(string bearerToken)
    {
        var client = httpClientFactory.CreateClient("BackendApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bearerToken);
        return client;
    }

    private static StringContent Serialize<T>(T body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<TResponse?> Deserialize<TResponse>(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
    }
}
