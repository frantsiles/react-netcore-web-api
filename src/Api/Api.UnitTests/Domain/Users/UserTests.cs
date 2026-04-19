using Api.Domain.Common;
using Api.Domain.Permissions;
using Api.Domain.Roles;
using Api.Domain.Users;
using FluentAssertions;

namespace Api.UnitTests.Domain.Users;

/// <summary>
/// Unit tests for the User aggregate.
/// These tests run in memory — no database, no HTTP, no infrastructure.
/// </summary>
public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateActiveUser()
    {
        // Arrange & Act
        var user = User.Create("John", "Doe", "john@example.com", "hashedPassword");

        // Assert
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Email.Value.Should().Be("john@example.com");
        user.IsActive.Should().BeTrue();
        user.Roles.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyFirstName_ShouldThrowDomainException(string firstName)
    {
        // Act
        var act = () => User.Create(firstName, "Doe", "john@example.com", "hash");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*First name*");
    }

    [Fact]
    public void AssignRole_ShouldAddRoleToUser()
    {
        // Arrange
        var user = User.Create("Jane", "Doe", "jane@example.com", "hash");
        var role = Role.Create("Admin", "Administrator role");

        // Act
        user.AssignRole(role);

        // Assert
        user.Roles.Should().ContainSingle(r => r.Name == "Admin");
    }

    [Fact]
    public void AssignRole_SameTwice_ShouldNotDuplicate()
    {
        // Arrange
        var user = User.Create("Jane", "Doe", "jane@example.com", "hash");
        var role = Role.Create("Admin", "Administrator role");

        // Act
        user.AssignRole(role);
        user.AssignRole(role);

        // Assert
        user.Roles.Should().HaveCount(1);
    }

    [Fact]
    public void HasPermission_WhenRoleHasPermission_ShouldReturnTrue()
    {
        // Arrange
        var user = User.Create("Jane", "Doe", "jane@example.com", "hash");
        var role = Role.Create("Admin", "Administrator role");
        var permission = Permission.Create("users", "read", "Read users");
        role.AddPermission(permission);
        user.AssignRole(role);

        // Act & Assert
        user.HasPermission("users:read").Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var user = User.Create("Jane", "Doe", "jane@example.com", "hash");

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }
}
