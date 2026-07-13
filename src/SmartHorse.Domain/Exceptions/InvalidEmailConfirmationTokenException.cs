namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a confirmation token is missing, expired, or does not match. Mapped to HTTP 400.</summary>
public class InvalidEmailConfirmationTokenException : DomainException
{
    public InvalidEmailConfirmationTokenException()
        : base("The email confirmation token is invalid or has expired.")
    {
    }
}
