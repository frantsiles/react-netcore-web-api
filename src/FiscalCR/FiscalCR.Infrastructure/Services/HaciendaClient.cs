using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiscalCR.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FiscalCR.Infrastructure.Services;

/// <summary>
/// Real Hacienda ATV client.
/// Sandbox:    https://api-sandbox.comprobanteselectronicos.go.cr/recepcion/v1/
/// Production: https://api.comprobanteselectronicos.go.cr/recepcion/v1/
/// Token IDP:  https://idp.comprobanteselectronicos.go.cr/auth/realms/rut-stag/protocol/openid-connect/token (sandbox)
///             https://idp.comprobanteselectronicos.go.cr/auth/realms/rut/protocol/openid-connect/token (production)
/// </summary>
public class HaciendaClient(
    ITenantFiscalCrConfigRepository configRepo,
    IHttpClientFactory httpClientFactory,
    ILogger<HaciendaClient> logger) : IHaciendaClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<bool> SubmitAsync(HaciendaSubmitRequest request, Guid tenantId, CancellationToken ct = default)
    {
        var tenantConfig = await configRepo.FindByTenantIdAsync(tenantId, ct);

        if (tenantConfig?.HasHaciendaCredentials != true)
        {
            logger.LogWarning(
                "HaciendaClient: no Hacienda credentials for tenant {TenantId}. " +
                "Falling back to stub acceptance.", tenantId);
            return true;
        }

        try
        {
            var (baseUrl, _) = GetEndpoints(tenantConfig.Environment);
            var token = await GetAccessTokenAsync(tenantConfig.HaciendaUsername!, tenantConfig.HaciendaPassword!, tenantConfig.Environment, ct);

            var client = httpClientFactory.CreateClient("Hacienda");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new HaciendaRecepcionRequest
            {
                Clave = request.Clave,
                Fecha = request.Fecha,
                Emisor = new HaciendaIdentificacion(request.EmisorTipoId, request.EmisorCedula),
                Receptor = request.ReceptorTipoId is not null && request.ReceptorCedula is not null
                    ? new HaciendaIdentificacion(request.ReceptorTipoId, request.ReceptorCedula)
                    : null,
                ComprobanteXml = request.ComprobanteXmlBase64,
                CallbackUrl = null
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}recepcion", payload, JsonOpts, ct);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                logger.LogInformation("Hacienda accepted submission for clave {Clave}", request.Clave);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Hacienda rejected submission for clave {Clave}: {Status} {Body}",
                request.Clave, response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error submitting to Hacienda for clave {Clave}", request.Clave);
            return false;
        }
    }

    public async Task<HaciendaStatusResponse?> QueryStatusAsync(string clave, Guid tenantId, CancellationToken ct = default)
    {
        var tenantConfig = await configRepo.FindByTenantIdAsync(tenantId, ct);

        if (tenantConfig?.HasHaciendaCredentials != true)
        {
            return new HaciendaStatusResponse(clave, "aceptado",
                "Aceptado (sin credenciales configuradas — ambiente de desarrollo)", null);
        }

        try
        {
            var (baseUrl, _) = GetEndpoints(tenantConfig.Environment);
            var token = await GetAccessTokenAsync(tenantConfig.HaciendaUsername!, tenantConfig.HaciendaPassword!, tenantConfig.Environment, ct);

            var client = httpClientFactory.CreateClient("Hacienda");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{baseUrl}recepcion/{clave}", ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadFromJsonAsync<HaciendaEstadoResponse>(JsonOpts, ct);
            if (json is null) return null;

            return new HaciendaStatusResponse(
                clave,
                json.IndEstado ?? "procesando",
                json.RespuestaXml is not null ? ExtractMensaje(json.RespuestaXml) : null,
                json.RespuestaXml);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying Hacienda status for clave {Clave}", clave);
            return null;
        }
    }

    private async Task<string> GetAccessTokenAsync(string username, string password, string environment, CancellationToken ct)
    {
        var (_, idpUrl) = GetEndpoints(environment);
        var client = httpClientFactory.CreateClient("Hacienda");

        var form = new FormUrlEncodedContent([
            new("grant_type", "password"),
            new("client_id", "api-prod"),
            new("username", username),
            new("password", password)
        ]);

        var response = await client.PostAsync(idpUrl, form, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<HaciendaTokenResponse>(JsonOpts, ct)
            ?? throw new InvalidOperationException("Empty token response from Hacienda IDP.");

        return tokenResponse.AccessToken;
    }

    private static (string BaseUrl, string IdpUrl) GetEndpoints(string environment) =>
        environment.Equals("production", StringComparison.OrdinalIgnoreCase)
            ? ("https://api.comprobanteselectronicos.go.cr/recepcion/v1/",
               "https://idp.comprobanteselectronicos.go.cr/auth/realms/rut/protocol/openid-connect/token")
            : ("https://api-sandbox.comprobanteselectronicos.go.cr/recepcion/v1/",
               "https://idp.comprobanteselectronicos.go.cr/auth/realms/rut-stag/protocol/openid-connect/token");

    private static string? ExtractMensaje(string xmlRespuesta)
    {
        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xmlRespuesta);
            return doc.SelectSingleNode("//*[local-name()='DetalleMensaje']")?.InnerText;
        }
        catch { return null; }
    }

    // Internal DTOs for Hacienda API
    private sealed class HaciendaRecepcionRequest
    {
        [JsonPropertyName("clave")]        public string Clave { get; init; } = default!;
        [JsonPropertyName("fecha")]        public string Fecha { get; init; } = default!;
        [JsonPropertyName("emisor")]       public HaciendaIdentificacion? Emisor { get; init; }
        [JsonPropertyName("receptor")]     public HaciendaIdentificacion? Receptor { get; init; }
        [JsonPropertyName("comprobanteXml")] public string? ComprobanteXml { get; init; }
        [JsonPropertyName("callbackUrl")] public string? CallbackUrl { get; init; }
    }

    private record HaciendaIdentificacion(
        [property: JsonPropertyName("tipoIdentificacion")] string TipoIdentificacion,
        [property: JsonPropertyName("numeroIdentificacion")] string NumeroIdentificacion);

    private record HaciendaEstadoResponse(
        [property: JsonPropertyName("indEstado")] string? IndEstado,
        [property: JsonPropertyName("respuestaXml")] string? RespuestaXml);

    private record HaciendaTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
}
