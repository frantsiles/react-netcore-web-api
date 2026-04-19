using BFF.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BFF.Api.Controllers;

/// <summary>
/// React calls this endpoint to log in. The BFF forwards credentials to the backend API,
/// receives the JWT, and returns it to React. React stores it in sessionStorage.
///
/// Status codes mirror the backend API — the BFF is transparent about auth failures
/// so the React client can show the right error message.
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
            var result = await mediator.Send(new LoginBffCommand(request.Email, request.Password), ct);
            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Propagate 401 from backend — wrong credentials
            return Unauthorized(new { message = "Invalid email or password." });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            // Propagate 400 from backend — validation errors
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Backend API is unavailable.", detail = ex.Message });
        }
    }
}

public record LoginBffRequest(string Email, string Password);
