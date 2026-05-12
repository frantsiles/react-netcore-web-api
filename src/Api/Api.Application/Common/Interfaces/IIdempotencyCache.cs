namespace Api.Application.Common.Interfaces;

public record IdempotencyEntry(int StatusCode, string ContentType, string Body);

/// <summary>
/// Cache used by the idempotency middleware to deduplicate mutating requests
/// based on the X-Idempotency-Key header.
/// </summary>
public interface IIdempotencyCache
{
    Task<IdempotencyEntry?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, IdempotencyEntry entry, CancellationToken ct = default);
}
