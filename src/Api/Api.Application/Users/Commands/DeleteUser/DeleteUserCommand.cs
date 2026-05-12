using MediatR;

namespace Api.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId) : IRequest;
