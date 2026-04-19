using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Users;

public class GetUsersBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetUsersBffQuery, IReadOnlyList<UserBffDto>>
{
    public async Task<IReadOnlyList<UserBffDto>> Handle(GetUsersBffQuery request, CancellationToken ct)
    {
        var users = await apiClient.GetAsync<List<UserBffDto>>(
            "api/users",
            bearerToken: request.BearerToken,
            ct: ct);

        return users ?? [];
    }
}
