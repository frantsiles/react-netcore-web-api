using Api.Domain.Common;

namespace Api.Domain.Permissions;

/// <summary>
/// Represents a granular action that can be granted to a role.
/// Example: "users:read", "users:write", "roles:manage"
/// </summary>
public class Permission : Entity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Resource { get; private set; }
    public string Action { get; private set; }

    private Permission() : base() { Name = ""; Description = ""; Resource = ""; Action = ""; }

    private Permission(string name, string description, string resource, string action) : base()
    {
        Name = name;
        Description = description;
        Resource = resource;
        Action = action;
    }

    /// <summary>
    /// Creates a new permission using the "resource:action" naming convention.
    /// </summary>
    public static Permission Create(string resource, string action, string description)
    {
        if (string.IsNullOrWhiteSpace(resource)) throw new DomainException("Permission resource cannot be empty.");
        if (string.IsNullOrWhiteSpace(action)) throw new DomainException("Permission action cannot be empty.");

        var name = $"{resource.ToLowerInvariant()}:{action.ToLowerInvariant()}";
        return new Permission(name, description, resource.ToLowerInvariant(), action.ToLowerInvariant());
    }
}
