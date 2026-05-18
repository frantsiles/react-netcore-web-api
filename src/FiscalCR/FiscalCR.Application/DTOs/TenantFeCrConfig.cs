namespace FiscalCR.Application.DTOs;

/// <summary>Per-tenant Costa Rica fiscal configuration, loaded from appsettings or tenant settings.</summary>
public record TenantFeCrConfig(
    // Emisor identity
    string RazonSocial,
    string NombreComercial,
    string TipoIdentificacion,  // 01=física 02=jurídica
    string NumeroIdentificacion,
    string CodigoActividad,     // Hacienda economic activity code
    // Address
    string Provincia,
    string Canton,
    string Distrito,
    string OtrasSenas,
    // Contact
    string? Telefono,
    string Email,
    // Hacienda credentials (for real integration)
    string? HaciendaUsername,
    string? HaciendaPassword,
    string? CertificatePath,
    string? CertificatePassword);
