using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Users;

public record CreateUserBffCommand(
    string BearerToken,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string RoleName) : IRequest<UserBffDto>;

public record CreateUserBffPayload(string FirstName, string LastName, string Email, string Password, string RoleName);

public class CreateUserBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateUserBffCommand, UserBffDto>
{
    public async Task<UserBffDto> Handle(CreateUserBffCommand request, CancellationToken ct)
    {
        var dto = await apiClient.PostAsync<CreateUserBffPayload, UserBffDto>(
            "api/users",
            new CreateUserBffPayload(request.FirstName, request.LastName, request.Email, request.Password, request.RoleName),
            request.BearerToken,
            ct);

        return dto!;
    }
}
