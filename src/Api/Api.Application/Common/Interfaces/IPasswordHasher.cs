namespace Api.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the password hashing algorithm.
/// Defined in Application so domain/application code doesn't depend on BCrypt directly.
/// Implemented in Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}
