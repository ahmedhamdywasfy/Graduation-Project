namespace SmartHorse.Domain.Exceptions;

/// <summary>
/// Thrown on failed login attempts (unknown email or wrong password). Deliberately
/// generic in its message so the API never reveals whether the email exists.
/// Mapped to HTTP 401 by the global exception handling middleware.
/// </summary>
public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
