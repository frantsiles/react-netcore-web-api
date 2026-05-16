using Api.Domain.Common;
using System.Text.RegularExpressions;

namespace Parties.Domain.ValueObjects;

public sealed class ContactPoint : ValueObject
{
    private static readonly Regex _emailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _phoneRegex = new(
        @"^\+?[\d\s\-().]{7,20}$", RegexOptions.Compiled);

    public ContactPointType Type { get; private set; }
    public string Value { get; private set; } = "";
    public bool IsPrimary { get; private set; }

    private ContactPoint() { }

    private ContactPoint(ContactPointType type, string value, bool isPrimary)
    {
        Type = type;
        Value = value;
        IsPrimary = isPrimary;
    }

    public static ContactPoint Create(ContactPointType type, string value, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Contact point value cannot be empty.");

        var normalized = value.Trim();

        switch (type)
        {
            case ContactPointType.Email:
                if (!_emailRegex.IsMatch(normalized))
                    throw new DomainException($"'{value}' is not a valid email address.");
                normalized = normalized.ToLowerInvariant();
                break;

            case ContactPointType.Phone:
                if (!_phoneRegex.IsMatch(normalized))
                    throw new DomainException($"'{value}' is not a valid phone number.");
                break;
        }

        return new ContactPoint(type, normalized, isPrimary);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Value;
    }
}
