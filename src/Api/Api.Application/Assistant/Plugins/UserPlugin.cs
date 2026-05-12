using System.ComponentModel;
using System.Text.Json;
using Api.Application.Users.Commands.CreateUser;
using Api.Application.Users.Queries.GetUsers;
using MediatR;
using Microsoft.SemanticKernel;

namespace Api.Application.Assistant.Plugins;

public class UserPlugin(ISender sender)
{
    [KernelFunction("get_users")]
    [Description("Obtiene la lista de todos los usuarios del sistema con sus roles y estado")]
    public async Task<string> GetUsersAsync(CancellationToken ct = default)
    {
        IReadOnlyList<UserDto> users = await sender.Send(new GetUsersQuery(), ct);
        return JsonSerializer.Serialize(users);
    }

    [KernelFunction("create_user")]
    [Description("Crea un nuevo usuario en el sistema")]
    public async Task<string> CreateUserAsync(
        [Description("Nombre del usuario")] string firstName,
        [Description("Apellido del usuario")] string lastName,
        [Description("Email del usuario")] string email,
        [Description("Contraseña del usuario (mínimo 8 caracteres)")] string password,
        [Description("Rol del usuario: Admin o Viewer")] string roleName,
        CancellationToken ct = default)
    {
        UserDto created = await sender.Send(
            new CreateUserCommand(firstName, lastName, email, password, roleName), ct);
        return JsonSerializer.Serialize(new { success = true, user = created });
    }

    [KernelFunction("get_user_count")]
    [Description("Retorna el número total de usuarios activos en el sistema")]
    public async Task<string> GetUserCountAsync(CancellationToken ct = default)
    {
        IReadOnlyList<UserDto> users = await sender.Send(new GetUsersQuery(), ct);
        int activeCount = users.Count(u => u.IsActive);
        return activeCount.ToString();
    }
}
