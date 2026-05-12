using Api.Application.Users.Queries.GetUsers;
using MediatR;

namespace Api.Application.Users.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, string NewRoleName) : IRequest<UserDto>;
