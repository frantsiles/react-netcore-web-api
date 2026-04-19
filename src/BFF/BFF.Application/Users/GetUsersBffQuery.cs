using MediatR;

namespace BFF.Application.Users;

/// <summary>
/// Forwards the get-users request to the backend API, passing the bearer token.
/// </summary>
public record GetUsersBffQuery(string BearerToken) : IRequest<IReadOnlyList<UserBffDto>>;

public record UserBffDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    IEnumerable<string> Roles);
