using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

public record AskBffRequest(string Question);

[ApiController]
[Route("bff/[controller]")]
[Authorize]
public class AssistantController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpPost]
    public async Task AskAsync([FromBody] AskBffRequest request, CancellationToken ct)
    {
        string bearerToken = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        HttpClient client = httpClientFactory.CreateClient("BackendApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bearerToken);

        string json = JsonSerializer.Serialize(new { question = request.Question });
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, "api/assistant")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage apiResponse = await client.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        apiResponse.EnsureSuccessStatusCode();

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await using Stream apiStream = await apiResponse.Content.ReadAsStreamAsync(ct);
        await apiStream.CopyToAsync(Response.Body, ct);
    }
}
