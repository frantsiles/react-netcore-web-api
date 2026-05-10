using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions.App.Functions;

public class UserHttpFunction(ILogger<UserHttpFunction> logger)
{
    [Function("GetUsers")]
    public IActionResult GetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "functions/users")] HttpRequest req)
    {
        logger.LogInformation("GetUsers invoked from {RemoteIp}", req.HttpContext.Connection.RemoteIpAddress);

        var users = new[]
        {
            new { Id = Guid.NewGuid(), Name = "Demo Admin",  Role = "Admin", Source = "azure-function" },
            new { Id = Guid.NewGuid(), Name = "Demo User",   Role = "User",  Source = "azure-function" }
        };

        return new OkObjectResult(new { source = "azure-function", timestamp = DateTime.UtcNow, users });
    }

    [Function("GetUserById")]
    public IActionResult GetUserById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "functions/users/{id:guid}")] HttpRequest req,
        Guid id)
    {
        logger.LogInformation("GetUserById invoked — Id: {Id}", id);

        return new OkObjectResult(new
        {
            source = "azure-function",
            id,
            timestamp = DateTime.UtcNow,
            message = $"User {id} retrieved via Azure Function"
        });
    }
}
