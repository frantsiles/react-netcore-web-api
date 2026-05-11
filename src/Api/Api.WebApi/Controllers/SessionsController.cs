using Api.Application.Sessions.Commands;
using Api.Application.Sessions.Queries;
using Api.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController(IMediator mediator) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("User id claim missing."));

    private Guid CurrentSessionId => Guid.TryParse(User.FindFirstValue("sid"), out var sid)
        ? sid
        : Guid.Empty;

    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMySessionsQuery(CurrentUserId, CurrentSessionId), ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllActiveSessions(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllActiveSessionsQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
    {
        try
        {
            var isAdmin = User.IsInRole("Admin");
            await mediator.Send(new RevokeSessionCommand(id, CurrentUserId, isAdmin), ct);
            return NoContent();
        }
        catch (DomainException ex) when (ex.Message == "Access denied.")
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("my")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAllMySessions(CancellationToken ct)
    {
        await mediator.Send(new RevokeAllMySessionsCommand(CurrentUserId, CurrentSessionId), ct);
        return NoContent();
    }

    [HttpDelete("/api/admin/users/{userId:guid}/sessions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAllUserSessions(Guid userId, CancellationToken ct)
    {
        await mediator.Send(new RevokeAllUserSessionsCommand(userId, CurrentUserId), ct);
        return NoContent();
    }
}
