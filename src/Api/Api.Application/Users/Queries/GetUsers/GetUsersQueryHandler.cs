using Api.Domain.Users.Repositories;
using MediatR;

namespace Api.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var users = await userRepository.GetAllAsync(ct);

        return users.Select(u => new UserDto(
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email.Value,
            u.IsActive,
            u.Roles.Select(r => r.Name)
        )).ToList();
    }
}
