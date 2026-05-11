using Api.Application.Auth.Commands.Login;
using Api.Application.Auth.Commands.Logout;
using Api.Application.Auth.Commands.RefreshToken;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await mediator.Send(
                new LoginCommand(request.Email, request.Password, userAgent, ipAddress), ct);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await mediator.Send(
                new RefreshTokenCommand(request.RefreshToken, userAgent, ipAddress), ct);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return Unauthorized(new { message = ex.Message, code = "token_expired" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (Guid.TryParse(userId, out var userGuid))
            await mediator.Send(new LogoutCommand(request.RefreshToken, userGuid), ct);

        return NoContent();
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
