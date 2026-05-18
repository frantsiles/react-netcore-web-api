namespace FiscalCR.Domain.Services;

/// <summary>Signs a CR FE XML document using a tenant's .p12 certificate (XAdES-BES).</summary>
public interface IXmlSigner
{
    /// <summary>Returns the signed XML string.</summary>
    Task<string> SignAsync(string xmlContent, Guid tenantId, CancellationToken ct = default);
}
