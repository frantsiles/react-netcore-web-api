using Api.Application.Assistant;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssistantController(IAssistantService assistantService) : ControllerBase
{
    [HttpPost]
    public async Task AskAsync(
        [FromBody] AskRequest request,
        [FromServices] IValidator<AskRequest> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                validation.Errors.Select(e => e.ErrorMessage), ct);
            return;
        }

        Guid requesterId = Guid.TryParse(
            User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            out Guid parsed) ? parsed : Guid.Empty;

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            await foreach (string chunk in assistantService.AskStreamingAsync(request.Question, requesterId, ct))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // cliente desconectado — salir sin error
        }
    }
}
