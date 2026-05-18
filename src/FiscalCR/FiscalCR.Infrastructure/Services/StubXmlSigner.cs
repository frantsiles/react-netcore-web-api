using FiscalCR.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FiscalCR.Infrastructure.Services;

/// <summary>
/// Development stub: returns the XML as-is without signing.
/// Replace with a real XAdES-BES implementation for production.
/// Production requires: System.Security.Cryptography.Xml + tenant .p12 certificate.
/// </summary>
public class StubXmlSigner(ILogger<StubXmlSigner> logger) : IXmlSigner
{
    public Task<string> SignAsync(string xmlContent, Guid tenantId, CancellationToken ct = default)
    {
        logger.LogWarning(
            "StubXmlSigner: returning unsigned XML for tenant {TenantId}. " +
            "Configure a real XAdES-BES signer for production.", tenantId);
        return Task.FromResult(xmlContent);
    }
}
