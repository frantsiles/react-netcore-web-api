using MediatR;

namespace Api.Application.Users.Queries.GetUsers;

/// <summary>Query: returns the list of all users with their roles.</summary>
public record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    IEnumerable<string> Roles);
