using Api.Application.Users.Commands.ChangeUserRole;
using Api.Application.Users.Commands.CreateUser;
using Api.Application.Users.Commands.DeleteUser;
using Api.Application.Users.Queries.GetUsers;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await mediator.Send(new GetUsersQuery(), ct);
        return Ok(users);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        try
        {
            var dto = await mediator.Send(
                new CreateUserCommand(request.FirstName, request.LastName, request.Email, request.Password, request.RoleName),
                ct);

            return CreatedAtAction(nameof(GetAll), new { id = dto.Id }, dto);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteUserCommand(id), ct);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request, CancellationToken ct)
    {
        try
        {
            var dto = await mediator.Send(new ChangeUserRoleCommand(id, request.RoleName), ct);
            return Ok(dto);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record CreateUserRequest(string FirstName, string LastName, string Email, string Password, string RoleName);
public record ChangeRoleRequest(string RoleName);
