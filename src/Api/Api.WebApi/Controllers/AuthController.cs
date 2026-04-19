using Api.Application.Auth.Commands.Login;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

/// <summary>
/// Handles authentication. This is the only endpoint that does NOT require a JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Authenticates a user and returns a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password), ct);
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
}

public record LoginRequest(string Email, string Password);
