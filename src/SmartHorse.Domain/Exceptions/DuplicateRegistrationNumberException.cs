namespace SmartHorse.Domain.Exceptions;

/// <summary>Thrown when a registration number is already registered to another horse. Mapped to HTTP 409.</summary>
public class DuplicateRegistrationNumberException : DomainException
{
    public DuplicateRegistrationNumberException(string registrationNumber)
        : base($"Registration number \"{registrationNumber}\" is already registered to another horse.")
    {
    }
}
