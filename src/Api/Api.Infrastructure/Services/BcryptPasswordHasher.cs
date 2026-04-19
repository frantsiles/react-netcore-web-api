using Api.Application.Common.Interfaces;

namespace Api.Infrastructure.Services;

/// <summary>
/// BCrypt implementation of IPasswordHasher.
/// Work factor 12 is the recommended balance of security vs. performance as of 2024.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);

    public bool Verify(string plainPassword, string hash)
        => BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}
