namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a confirmation/resend is attempted on an already-confirmed email. Mapped to HTTP 409.</summary>
public class EmailAlreadyConfirmedException : DomainException
{
    public EmailAlreadyConfirmedException()
        : base("This email address has already been confirmed.")
    {
    }
}
