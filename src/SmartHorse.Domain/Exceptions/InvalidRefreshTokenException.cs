namespace SmartHorse.Domain.Exceptions;

/// <summary>
/// Thrown when a refresh token is missing, expired, revoked, or already replaced
/// (reuse of a rotated token — see v0.2 Security Review, Section 8). Reuse detection
/// additionally revokes the entire token chain for the user as a precaution.
/// Mapped to HTTP 401 by the global exception handling middleware.
/// </summary>
public class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("Refresh token is invalid, expired, or has already been used.")
    {
    }
}
