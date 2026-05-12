using System.ComponentModel;
using System.Text.Json;
using Api.Application.Sessions.Commands;
using Api.Application.Sessions.Queries;
using MediatR;
using Microsoft.SemanticKernel;

namespace Api.Application.Assistant.Plugins;

public class SessionPlugin(ISender sender)
{
    [KernelFunction("get_active_sessions")]
    [Description("Obtiene todas las sesiones activas del sistema con información del usuario y dispositivo")]
    public async Task<string> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<SessionAdminDto> sessions =
            await sender.Send(new GetAllActiveSessionsQuery(), ct);
        return JsonSerializer.Serialize(sessions);
    }

    [KernelFunction("revoke_session")]
    [Description("Revoca una sesión activa por su ID. Usar cuando se pide cerrar o revocar una sesión específica")]
    public async Task<string> RevokeSessionAsync(
        [Description("ID de la sesión a revocar (GUID)")] string sessionId,
        [Description("ID del usuario que hace la revocación (GUID)")] string requesterId,
        CancellationToken ct = default)
    {
        Guid sessionGuid = Guid.Parse(sessionId);
        Guid requesterGuid = Guid.Parse(requesterId);

        await sender.Send(
            new RevokeSessionCommand(sessionGuid, requesterGuid, RequesterIsAdmin: true), ct);

        return JsonSerializer.Serialize(new { success = true, revokedSessionId = sessionId });
    }

    [KernelFunction("get_session_count")]
    [Description("Retorna el número de sesiones activas en este momento")]
    public async Task<string> GetSessionCountAsync(CancellationToken ct = default)
    {
        IReadOnlyList<SessionAdminDto> sessions =
            await sender.Send(new GetAllActiveSessionsQuery(), ct);
        return sessions.Count.ToString();
    }
}
