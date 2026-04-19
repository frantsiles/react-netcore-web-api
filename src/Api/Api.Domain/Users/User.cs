using Api.Domain.Common;
using Api.Domain.Roles;
using Api.Domain.Users.ValueObjects;

namespace Api.Domain.Users;

/// <summary>
/// User aggregate root.
/// Owns the collection of assigned roles and enforces business rules around
/// identity, activation, and role assignment.
/// </summary>
public class User : Entity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Role> _roles = [];
    public IReadOnlyList<Role> Roles => _roles.AsReadOnly();

    // EF Core requires a parameterless constructor
    private User() : base() { FirstName = ""; LastName = ""; Email = null!; PasswordHash = null!; }

    private User(string firstName, string lastName, Email email, PasswordHash passwordHash) : base()
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public static User Create(string firstName, string lastName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name cannot be empty.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name cannot be empty.");

        return new User(
            firstName.Trim(),
            lastName.Trim(),
            Email.Create(email),
            PasswordHash.FromHash(passwordHash));
    }

    public void AssignRole(Role role)
    {
        if (_roles.Any(r => r.Id == role.Id)) return;
        _roles.Add(role);
        SetUpdatedAt();
    }

    public void RemoveRole(Guid roleId)
    {
        var role = _roles.FirstOrDefault(r => r.Id == roleId);
        if (role is not null)
        {
            _roles.Remove(role);
            SetUpdatedAt();
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }

    public bool HasPermission(string permissionName)
        => _roles.Any(r => r.HasPermission(permissionName));

    public string FullName => $"{FirstName} {LastName}";
}
