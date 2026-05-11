using BFF.Application.Sessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/[controller]")]
[Authorize]
public class SessionsController(IMediator mediator) : ControllerBase
{
    private string BearerToken =>
        Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionBffDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMySessionsBffQuery(BearerToken), ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionAdminBffDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSessions(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllSessionsBffQuery(BearerToken), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RevokeSessionBffCommand(id, BearerToken), ct);
            return NoContent();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Backend API is unavailable.", detail = ex.Message });
        }
    }

    [HttpDelete("my")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAllMySessions(CancellationToken ct)
    {
        await mediator.Send(new RevokeAllMySessionsBffCommand(BearerToken), ct);
        return NoContent();
    }

    [HttpDelete("/bff/admin/users/{userId:guid}/sessions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAllUserSessions(Guid userId, CancellationToken ct)
    {
        await mediator.Send(new RevokeAllUserSessionsBffCommand(userId, BearerToken), ct);
        return NoContent();
    }
}
