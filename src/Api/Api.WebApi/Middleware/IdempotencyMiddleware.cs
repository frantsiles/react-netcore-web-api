using Api.Application.Common.Interfaces;

namespace Api.WebApi.Middleware;

public class IdempotencyMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Idempotency-Key";

    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete
    };

    public async Task InvokeAsync(HttpContext context, IIdempotencyCache cache)
    {
        if (!MutatingMethods.Contains(context.Request.Method) ||
            !context.Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            await next(context);
            return;
        }

        string? key = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            await next(context);
            return;
        }

        string cacheKey = $"{context.Request.Method}:{context.Request.Path}:{key}";

        var cached = await cache.GetAsync(cacheKey, context.RequestAborted);
        if (cached is not null)
        {
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.WriteAsync(cached.Body, context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);

            buffer.Position = 0;
            string body = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var entry = new IdempotencyEntry(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    body);
                await cache.SetAsync(cacheKey, entry, context.RequestAborted);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
