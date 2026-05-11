using BFF.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BFF.Api.Controllers;

/// <summary>
/// React calls these endpoints. The BFF forwards credentials/tokens to the backend API.
/// Status codes mirror the backend API — the BFF is transparent about auth failures.
/// SignalR connects directly to the API (not via BFF) — documented in ADR-008.
/// </summary>
[ApiController]
[Route("bff/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginBffResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginBffRequest request, CancellationToken ct)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await mediator.Send(
                new LoginBffCommand(request.Email, request.Password, userAgent, ipAddress), ct);

            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Backend API is unavailable.", detail = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenBffResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshBffRequest request, CancellationToken ct)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await mediator.Send(
                new RefreshTokenBffCommand(request.RefreshToken, userAgent, ipAddress), ct);

            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Unauthorized(new { message = "Session expired or revoked.", code = "token_expired" });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Backend API is unavailable.", detail = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutBffRequest request, CancellationToken ct)
    {
        try
        {
            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            await mediator.Send(new LogoutBffCommand(request.RefreshToken, token), ct);
        }
        catch (HttpRequestException)
        {
            // Logout is best-effort — always return 204 to the client
        }

        return NoContent();
    }
}

public record LoginBffRequest(string Email, string Password);
public record RefreshBffRequest(string RefreshToken);
public record LogoutBffRequest(string RefreshToken);
