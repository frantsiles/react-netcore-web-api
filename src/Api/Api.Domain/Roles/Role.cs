using Api.Domain.Common;
using Api.Domain.Permissions;

namespace Api.Domain.Roles;

/// <summary>
/// A role groups a set of permissions and is assigned to users.
/// Uses an aggregate pattern: Role owns its permission collection.
/// </summary>
public class Role : Entity
{
    public string Name { get; private set; }
    public string Description { get; private set; }

    private readonly List<Permission> _permissions = [];
    public IReadOnlyList<Permission> Permissions => _permissions.AsReadOnly();

    private Role() : base() { Name = ""; Description = ""; }

    private Role(string name, string description) : base()
    {
        Name = name;
        Description = description;
    }

    public static Role Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Role name cannot be empty.");
        return new Role(name.Trim(), description.Trim());
    }

    public void AddPermission(Permission permission)
    {
        if (_permissions.Any(p => p.Id == permission.Id)) return;
        _permissions.Add(permission);
        SetUpdatedAt();
    }

    public void RemovePermission(Guid permissionId)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        if (permission is not null)
        {
            _permissions.Remove(permission);
            SetUpdatedAt();
        }
    }

    public bool HasPermission(string permissionName)
        => _permissions.Any(p => p.Name == permissionName);
}
