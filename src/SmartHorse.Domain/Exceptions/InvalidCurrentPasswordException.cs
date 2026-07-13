namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown by Change Password when the supplied current password does not match. Mapped to HTTP 400.</summary>
public class InvalidCurrentPasswordException : DomainException
{
    public InvalidCurrentPasswordException()
        : base("The current password you entered is incorrect.")
    {
    }
}
