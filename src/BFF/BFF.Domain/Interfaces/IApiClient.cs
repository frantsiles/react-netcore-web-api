namespace BFF.Domain.Interfaces;

/// <summary>
/// Abstraction over HTTP calls to the backend API.
/// The BFF never talks to the database — it always goes through the API.
/// </summary>
public interface IApiClient
{
    Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, string bearerToken, CancellationToken ct = default);
    Task<TResponse?> GetAsync<TResponse>(string path, string? bearerToken = null, CancellationToken ct = default);
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string path, TRequest body, string bearerToken, CancellationToken ct = default);
    Task PatchAsync(string path, string bearerToken, CancellationToken ct = default);
    Task DeleteAsync(string path, string bearerToken, CancellationToken ct = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string path, TRequest body, string bearerToken, CancellationToken ct = default);
}
