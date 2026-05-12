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
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserBffDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await mediator.Send(new GetUsersBffQuery(GetToken()), ct);
        return Ok(users);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserBffDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserBffRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new CreateUserBffCommand(GetToken(), request.FirstName, request.LastName, request.Email, request.Password, request.RoleName),
            ct);

        return CreatedAtAction(nameof(GetAll), new { id = dto.Id }, dto);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteUserBffCommand(GetToken(), id), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(UserBffDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleBffRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(new ChangeUserRoleBffCommand(GetToken(), id, request.RoleName), ct);
        return Ok(dto);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreateUserBffRequest(string FirstName, string LastName, string Email, string Password, string RoleName);
public record ChangeRoleBffRequest(string RoleName);
