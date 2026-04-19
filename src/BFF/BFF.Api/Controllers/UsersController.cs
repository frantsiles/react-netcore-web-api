using BFF.Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/[controller]")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// The BFF extracts the bearer token from the incoming React request
    /// and forwards it to the backend API. This keeps React decoupled from the backend API URL.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserBffDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var token = HttpContext.Request.Headers.Authorization
            .ToString().Replace("Bearer ", "");

        var users = await mediator.Send(new GetUsersBffQuery(token), ct);
        return Ok(users);
    }
}
