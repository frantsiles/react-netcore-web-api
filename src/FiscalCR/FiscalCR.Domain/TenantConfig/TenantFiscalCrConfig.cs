namespace FiscalCR.Domain.TenantConfig;

/// <summary>
/// Per-tenant Costa Rica electronic invoicing configuration.
/// Stores Hacienda credentials and signing certificate as encrypted bytes.
/// </summary>
public class TenantFiscalCrConfig
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    // Emisor identity
    public string RazonSocial { get; private set; } = default!;
    public string NombreComercial { get; private set; } = default!;
    public string TipoIdentificacion { get; private set; } = default!; // 01=física 02=jurídica
    public string NumeroIdentificacion { get; private set; } = default!;
    public string CodigoActividad { get; private set; } = default!;

    // Address
    public string Provincia { get; private set; } = default!;
    public string Canton { get; private set; } = default!;
    public string Distrito { get; private set; } = default!;
    public string OtrasSenas { get; private set; } = default!;

    // Contact
    public string? Telefono { get; private set; }
    public string Email { get; private set; } = default!;

    // Hacienda ATV OAuth2 credentials
    public string? HaciendaUsername { get; private set; }
    public string? HaciendaPassword { get; private set; }

    // Signing certificate (.p12 bytes — encrypted at rest)
    public byte[]? CertificateBytes { get; private set; }
    public string? CertificatePassword { get; private set; }

    // Environment: "sandbox" or "production"
    public string Environment { get; private set; } = "sandbox";

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TenantFiscalCrConfig() { }

    public static TenantFiscalCrConfig Create(
        Guid tenantId,
        string razonSocial,
        string nombreComercial,
        string tipoIdentificacion,
        string numeroIdentificacion,
        string codigoActividad,
        string provincia,
        string canton,
        string distrito,
        string otrasSenas,
        string email,
        string? telefono = null,
        string? haciendaUsername = null,
        string? haciendaPassword = null,
        byte[]? certificateBytes = null,
        string? certificatePassword = null,
        string environment = "sandbox")
    {
        var now = DateTime.UtcNow;
        return new TenantFiscalCrConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RazonSocial = razonSocial,
            NombreComercial = nombreComercial,
            TipoIdentificacion = tipoIdentificacion,
            NumeroIdentificacion = numeroIdentificacion,
            CodigoActividad = codigoActividad,
            Provincia = provincia,
            Canton = canton,
            Distrito = distrito,
            OtrasSenas = otrasSenas,
            Email = email,
            Telefono = telefono,
            HaciendaUsername = haciendaUsername,
            HaciendaPassword = haciendaPassword,
            CertificateBytes = certificateBytes,
            CertificatePassword = certificatePassword,
            Environment = environment,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateCredentials(
        string? haciendaUsername,
        string? haciendaPassword,
        byte[]? certificateBytes,
        string? certificatePassword)
    {
        HaciendaUsername = haciendaUsername;
        HaciendaPassword = haciendaPassword;
        CertificateBytes = certificateBytes;
        CertificatePassword = certificatePassword;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasCertificate => CertificateBytes is { Length: > 0 };
    public bool HasHaciendaCredentials => !string.IsNullOrEmpty(HaciendaUsername);
}
