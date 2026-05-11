using Api.Domain.Users;

namespace Api.Application.Common.Interfaces;

/// <summary>
/// Generates a signed JWT for a given user.
/// Defined in Application; implemented in Infrastructure.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(User user, Guid sessionId);
}
