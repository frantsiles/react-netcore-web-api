using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Users;

public record DeleteUserBffCommand(string BearerToken, Guid UserId) : IRequest;

public class DeleteUserBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<DeleteUserBffCommand>
{
    public Task Handle(DeleteUserBffCommand request, CancellationToken ct)
        => apiClient.DeleteAsync($"api/users/{request.UserId}", request.BearerToken, ct);
}
