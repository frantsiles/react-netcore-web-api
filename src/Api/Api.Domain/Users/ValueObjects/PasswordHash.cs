using Api.Domain.Common;

namespace Api.Domain.Users.ValueObjects;

/// <summary>
/// Wraps a bcrypt password hash. The domain never stores raw passwords.
/// Hashing is done in Infrastructure; the domain only validates that a hash exists.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    public string Value { get; private set; } = "";

    // Required by EF Core owned-entity materialization
    private PasswordHash() { }

    private PasswordHash(string hash) => Value = hash;

    /// <summary>Creates a PasswordHash from an already-hashed string (from Infrastructure).</summary>
    public static PasswordHash FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new DomainException("Password hash cannot be empty.");
        return new PasswordHash(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
