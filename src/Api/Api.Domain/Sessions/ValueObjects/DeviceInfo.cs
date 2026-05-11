using Api.Domain.Common;

namespace Api.Domain.Sessions.ValueObjects;

public class DeviceInfo : ValueObject
{
    public string UserAgent { get; }
    public string IpAddress { get; }

    private DeviceInfo(string userAgent, string ipAddress)
    {
        UserAgent = userAgent;
        IpAddress = ipAddress;
    }

    public static DeviceInfo Create(string userAgent, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("IP address cannot be empty.");

        return new DeviceInfo(
            (userAgent ?? string.Empty).Truncate(500),
            ipAddress.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserAgent;
        yield return IpAddress;
    }
}

file static class StringExtensions
{
    internal static string Truncate(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
