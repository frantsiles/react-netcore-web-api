using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Api.WebApi.Hubs;

[Authorize]
public class SessionHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Context.User?.FindFirstValue("sub");

        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
        => base.OnDisconnectedAsync(exception);

    public async Task JoinAdminGroup()
    {
        var isAdmin = Context.User?.IsInRole("Admin") ?? false;
        if (isAdmin)
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin-sessions");
    }
}
