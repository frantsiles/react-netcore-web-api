namespace Api.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    string CountryCode { get; }
}
