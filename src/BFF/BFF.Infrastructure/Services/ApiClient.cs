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
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(path, content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string path, string? bearerToken = null, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("BackendApi");

        if (!string.IsNullOrWhiteSpace(bearerToken))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await client.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
    }
}
