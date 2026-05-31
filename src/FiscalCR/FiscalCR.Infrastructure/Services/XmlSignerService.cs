using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using FiscalCR.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FiscalCR.Infrastructure.Services;

/// <summary>
/// Signs CR FE XML using an enveloped XMLDSig (RSA-SHA256 + SHA-256 digest).
/// Hacienda FE 4.3 spec: signature is appended as ds:Signature inside the root element.
/// The certificate is loaded from the tenant's .p12 bytes stored in the DB.
/// </summary>
public class XmlSignerService(
    ITenantFiscalCrConfigRepository configRepo,
    ILogger<XmlSignerService> logger) : IXmlSigner
{
    public async Task<string> SignAsync(string xmlContent, Guid tenantId, CancellationToken ct = default)
    {
        var tenantConfig = await configRepo.FindByTenantIdAsync(tenantId, ct);

        X509Certificate2? cert = null;

        if (tenantConfig?.HasCertificate == true)
        {
            cert = LoadCertificateFromBytes(tenantConfig.CertificateBytes!, tenantConfig.CertificatePassword);
        }

        if (cert is null)
        {
            logger.LogWarning(
                "XmlSignerService: no certificate configured for tenant {TenantId}. " +
                "Returning unsigned XML. Configure a .p12 certificate to enable real signing.", tenantId);
            return xmlContent;
        }

        return Sign(xmlContent, cert);
    }

    private static X509Certificate2 LoadCertificateFromBytes(byte[] bytes, string? password)
    {
        // X509KeyStorageFlags.EphemeralKeySet avoids writing to the key store on Linux/Docker.
        var flags = X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable;
        return new X509Certificate2(bytes, password, flags);
    }

    private static string Sign(string xmlContent, X509Certificate2 cert)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlContent);

        var rsa = cert.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate does not contain an RSA private key.");

        var signedXml = new SignedXml(doc)
        {
            SigningKey = rsa
        };

        // Reference: enveloped signature over the entire document
        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
        signedXml.AddReference(reference);

        // Signature method: RSA-SHA256
        signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;

        // Include the certificate in the KeyInfo so Hacienda can validate it
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();

        var signatureElement = signedXml.GetXml();
        doc.DocumentElement!.AppendChild(doc.ImportNode(signatureElement, true));

        return doc.OuterXml;
    }
}
