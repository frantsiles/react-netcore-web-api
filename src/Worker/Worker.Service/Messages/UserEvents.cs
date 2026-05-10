namespace Worker.Service.Messages;

public record UserCreated(Guid UserId, string Email, string Role, DateTime CreatedAt);
public record UserDeleted(Guid UserId, DateTime DeletedAt);
public record UserRoleChanged(Guid UserId, string OldRole, string NewRole, DateTime ChangedAt);
