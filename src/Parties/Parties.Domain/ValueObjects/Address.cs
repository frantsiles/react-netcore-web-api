using Api.Domain.Common;

namespace Parties.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string Line1 { get; private set; } = "";
    public string? Line2 { get; private set; }
    public string City { get; private set; } = "";
    public string StateOrRegion { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string CountryCode { get; private set; } = "";
    public bool IsPrimary { get; private set; }

    private Address() { }

    private Address(string line1, string? line2, string city, string stateOrRegion,
        string postalCode, string countryCode, bool isPrimary)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        StateOrRegion = stateOrRegion;
        PostalCode = postalCode;
        CountryCode = countryCode.ToUpperInvariant();
        IsPrimary = isPrimary;
    }

    public static Address Create(string line1, string? line2, string city, string stateOrRegion,
        string postalCode, string countryCode, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(line1)) throw new DomainException("Address line 1 cannot be empty.");
        if (string.IsNullOrWhiteSpace(city)) throw new DomainException("City cannot be empty.");
        if (string.IsNullOrWhiteSpace(stateOrRegion)) throw new DomainException("State or region cannot be empty.");
        if (string.IsNullOrWhiteSpace(postalCode)) throw new DomainException("Postal code cannot be empty.");
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new DomainException("Country code must be a 2-letter ISO code.");

        return new Address(line1.Trim(), line2?.Trim(), city.Trim(),
            stateOrRegion.Trim(), postalCode.Trim(), countryCode.Trim(), isPrimary);
    }

    public Address AsPrimary() => new(Line1, Line2, City, StateOrRegion, PostalCode, CountryCode, true);
    public Address AsSecondary() => new(Line1, Line2, City, StateOrRegion, PostalCode, CountryCode, false);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return Line2;
        yield return City;
        yield return StateOrRegion;
        yield return PostalCode;
        yield return CountryCode;
    }
}
