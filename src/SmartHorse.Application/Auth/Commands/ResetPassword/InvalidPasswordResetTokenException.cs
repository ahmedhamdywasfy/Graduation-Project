using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Auth.Commands.ResetPassword;

/// <summary>Thrown when a reset token is missing, expired, or does not match. Mapped to HTTP 400.</summary>
public class InvalidPasswordResetTokenException : DomainException
{
    public InvalidPasswordResetTokenException()
        : base("The password reset token is invalid or has expired.")
    {
    }
}
