namespace Api.Domain.Common;

/// <summary>
/// Exception thrown when a domain rule is violated.
/// These are expected errors (business rule failures), not system errors.
/// </summary>
public class DomainException(string message) : Exception(message);
