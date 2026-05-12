using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Users;

public record ChangeUserRoleBffCommand(string BearerToken, Guid UserId, string RoleName) : IRequest<UserBffDto>;

public record ChangeRoleBffPayload(string RoleName);

public class ChangeUserRoleBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ChangeUserRoleBffCommand, UserBffDto>
{
    public async Task<UserBffDto> Handle(ChangeUserRoleBffCommand request, CancellationToken ct)
    {
        var dto = await apiClient.PatchAsync<ChangeRoleBffPayload, UserBffDto>(
            $"api/users/{request.UserId}/role",
            new ChangeRoleBffPayload(request.RoleName),
            request.BearerToken,
            ct);

        return dto!;
    }
}
