using Api.Domain.Common;
using System.Text.RegularExpressions;

namespace Parties.Domain.ValueObjects;

public sealed class TaxId : ValueObject
{
    private static readonly Dictionary<string, Regex> _patterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = new Regex(@"^\d{2}-?\d{7}$", RegexOptions.Compiled),
        ["MX"] = new Regex(@"^[A-Z&Ñ]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        ["ES"] = new Regex(@"^[A-Z0-9]\d{7}[A-Z0-9]$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        ["GT"] = new Regex(@"^\d{7,8}-\d$", RegexOptions.Compiled),
    };

    public string Value { get; private set; } = "";
    public string CountryCode { get; private set; } = "";

    private TaxId() { }

    private TaxId(string value, string countryCode)
    {
        Value = value;
        CountryCode = countryCode.ToUpperInvariant();
    }

    public static TaxId Create(string value, string countryCode)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Tax ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new DomainException("Country code must be a 2-letter ISO code.");

        var normalized = value.Trim().ToUpperInvariant();
        var country = countryCode.Trim().ToUpperInvariant();

        if (_patterns.TryGetValue(country, out var regex) && !regex.IsMatch(normalized))
            throw new DomainException($"Tax ID '{value}' is not valid for country '{country}'.");

        return new TaxId(normalized, country);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return CountryCode;
    }

    public override string ToString() => $"{Value} ({CountryCode})";
}
