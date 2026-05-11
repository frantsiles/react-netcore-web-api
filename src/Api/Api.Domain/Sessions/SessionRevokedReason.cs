namespace Api.Domain.Sessions;

public enum SessionRevokedReason
{
    Logout,
    AdminRevoked,
    TokenRotation,
    UserBanned
}
