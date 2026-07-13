namespace SmartHorse.Domain.Exceptions;

/// <summary>
/// Thrown when an authentication or refresh attempt targets a deactivated account.
/// Mapped to HTTP 403 by the global exception handling middleware.
/// </summary>
public class AccountInactiveException : DomainException
{
    public AccountInactiveException()
        : base("This account has been deactivated. Contact an administrator.")
    {
    }
}
