using Api.Application.Users.Queries.GetUsers;
using MediatR;

namespace Api.Application.Users.Commands.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string RoleName) : IRequest<UserDto>;
